// Katana/HeatBlade — URP-шейдер клинка, который нагревается от блоков.
//
// Идея:
//   * _Heat (0..1) — единственный «вход» от геймплея. Его пишет KatanaHeat.cs
//     через MaterialPropertyBlock, поэтому материал не инстанцируется.
//   * 0.00–0.35 — отпускная побежалость: соломенный → бронзовый оттенок,
//     свечения ещё нет.
//   * 0.35–0.70 — фиолетово-синий отпуск + первое тёмно-красное свечение кромки.
//   * 0.70–1.00 — кромка раскаляется до оранжево-жёлтого, появляется
//     мерцание, дрожание нормалей и усиленный fresnel.
//   * Хамона (волнистая линия закалки) подсвечивается отдельно и раньше
//     остального металла — она читается как «линия жара».
//
// Требования к мешу: обычные нормали. UV-развёртка желательна такая, чтобы
// U шёл поперёк клинка (0 — кромка, 1 — обух). Если развёртка другая —
// выключите хамона (_HamonBoost = 0) или подгоните _HamonLine.
//
// Совместимость: URP 12+ (Unity 2021.3 LTS и новее), Forward-рендер.

Shader "Katana/HeatBlade"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (0.74, 0.76, 0.80, 1)
        _Metallic("Metallic", Range(0, 1)) = 0.92
        _Smoothness("Smoothness", Range(0, 1)) = 0.86

        [NoScaleOffset] _HeatMask("Heat Mask (R = hot areas)", 2D) = "white" {}

        [Header(Heat)]
        _Heat("Heat (driver, 0..1)", Range(0, 1)) = 0
        _HeatEmissive("Emissive Strength", Range(0, 8)) = 3.0
        _HeatRimPower("Rim Power", Range(0.5, 8)) = 2.4
        _HeatRimBoost("Rim Boost", Range(0, 4)) = 1.0
        _HotColorLow("Hot Color Low", Color) = (0.55, 0.10, 0.01, 1)
        _HotColorMid("Hot Color Mid", Color) = (1.0, 0.33, 0.02, 1)
        _HotColorHigh("Hot Color High", Color) = (1.0, 0.84, 0.50, 1)

        [Header(Hamon)]
        _HamonLine("Hamon Line (UV.x)", Range(0, 0.5)) = 0.17
        _HamonWidth("Hamon Width", Range(0.002, 0.2)) = 0.05
        _HamonWaveScale("Hamon Wave Scale", Float) = 9
        _HamonWaveStrength("Hamon Wave Strength", Range(0, 0.2)) = 0.05
        _HamonBoost("Hamon Emission Boost", Range(0, 4)) = 2.2

        [Header(Shimmer)]
        _ShimmerStrength("Shimmer Normal Strength", Range(0, 1)) = 0.45
        _ShimmerSpeed("Shimmer Speed", Float) = 1.7
        _FlickerSpeed("Flicker Speed", Float) = 7
        _FlickerStrength("Flicker Strength", Range(0, 1)) = 0.18

        [Header(Tempering)]
        _TemperStrength("Tempering Tint Strength", Range(0, 1)) = 0.6

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _Metallic;
            half   _Smoothness;
            half   _Heat;
            half   _HeatEmissive;
            half   _HeatRimPower;
            half   _HeatRimBoost;
            half4  _HotColorLow;
            half4  _HotColorMid;
            half4  _HotColorHigh;
            half   _HamonLine;
            half   _HamonWidth;
            half   _HamonWaveScale;
            half   _HamonWaveStrength;
            half   _HamonBoost;
            half   _ShimmerStrength;
            half   _ShimmerSpeed;
            half   _FlickerSpeed;
            half   _FlickerStrength;
            half   _TemperStrength;
            half   _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
        TEXTURE2D(_HeatMask);  SAMPLER(sampler_HeatMask);

        // --- дешёвый value noise (без текстур) -------------------------------
        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float ValueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = Hash21(i);
            float b = Hash21(i + float2(1.0, 0.0));
            float c = Hash21(i + float2(0.0, 1.0));
            float d = Hash21(i + float2(1.0, 1.0));
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        float Fbm(float2 p)
        {
            return 0.60 * ValueNoise(p)
                 + 0.28 * ValueNoise(p * 2.03 + 17.1)
                 + 0.12 * ValueNoise(p * 4.01 + 3.7);
        }

        // --- цвет отпуска: сталь → солома → бронза → фиолет → синий → окалина
        half3 TemperTint(half h)
        {
            half3 tint = half3(1.0, 1.0, 1.0);
            tint = lerp(tint, half3(1.00, 0.82, 0.44), smoothstep(0.04, 0.18, h));
            tint = lerp(tint, half3(0.88, 0.50, 0.22), smoothstep(0.18, 0.30, h));
            tint = lerp(tint, half3(0.62, 0.34, 0.70), smoothstep(0.30, 0.44, h));
            tint = lerp(tint, half3(0.30, 0.46, 0.92), smoothstep(0.44, 0.60, h));
            tint = lerp(tint, half3(0.34, 0.30, 0.30), smoothstep(0.62, 0.82, h));
            return tint;
        }

        // --- собственное свечение раскалённого металла ----------------------
        half3 HeatEmission(half h)
        {
            half e = saturate((h - 0.40) / 0.60);
            half3 c = lerp(_HotColorLow.rgb, _HotColorMid.rgb, smoothstep(0.0, 0.55, e));
            c = lerp(c, _HotColorHigh.rgb, smoothstep(0.55, 1.0, e));
            return c * pow(e, 1.7);
        }

        // --- линия хамона: волнистая полоса ближе к кромке ------------------
        half HamonMask(float2 uv)
        {
            // HaMON идёт вдоль клинка, поэтому «поперёк» = min(u, 1-u):
            // работает и на верхней, и на нижней грани.
            float across = min(uv.x, 1.0 - uv.x);
            float wave = Fbm(float2(uv.y * _HamonWaveScale, uv.x * 2.3));
            float line = abs(across - (_HamonLine + (wave - 0.5) * _HamonWaveStrength * 2.0));
            return 1.0 - smoothstep(0.0, max(_HamonWidth, 1e-4), line);
        }

        // --- металлический GGX-шейдинг --------------------------------------
        half3 ShadePBR(half3 albedo, half3 N, half3 V, half3 L, half3 lightColor,
                       half metallic, half smoothness)
        {
            half NdotL = saturate(dot(N, L));
            half NdotV = saturate(dot(N, V)) + 1e-4;
            half3 H = normalize(L + V);
            half NdotH = saturate(dot(N, H));
            half VdotH = saturate(dot(V, H));

            half roughness = max(1.0 - smoothness, 0.045);
            half a  = roughness * roughness;
            half a2 = a * a;
            half d  = NdotH * NdotH * (a2 - 1.0) + 1.0;
            half D  = a2 / max(PI * d * d, 1e-6);
            half k  = a * 0.5;
            half vis = 0.25 / max((NdotL * (1.0 - k) + k) * (NdotV * (1.0 - k) + k), 1e-5);

            half3 F0 = lerp(0.04, albedo, metallic);
            half3 F  = F0 + (1.0 - F0) * pow(1.0 - VdotH, 5.0);

            half3 spec = D * vis * F;
            half3 diff = albedo * (1.0 - metallic) * (1.0 / PI);
            return (diff + spec) * NdotL * lightColor;
        }
        ENDHLSL

        // ====================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 uvRaw      : TEXCOORD4;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS);

                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uvRaw = v.uv;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                half3 N = normalize(i.normalWS);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 albedo = baseTex.rgb * _BaseColor.rgb;

                half mask = SAMPLE_TEXTURE2D(_HeatMask, sampler_HeatMask, i.uvRaw).r;
                half h = saturate(_Heat);

                // 1. Отпускные цвета металла — меняются даже при лёгком нагреве.
                albedo = lerp(albedo, albedo * TemperTint(h), _TemperStrength * mask);

                // 2. Дрожание нормалей: «металл плывёт от жара». Дёшево, без
                //    второй текстуры, и не ломает тени.
                float2 nUv = i.uvRaw * 7.0 + _Time.y * _ShimmerSpeed * float2(0.13, -0.31);
                float2 wobble = float2(Fbm(nUv), Fbm(nUv + 19.7)) - 0.5;
                N = normalize(N + half3(wobble.x, wobble.y, wobble.x - wobble.y)
                                  * (_ShimmerStrength * h * h * 0.45));

                // 3. Прямой свет + тени + доп. источники.
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 color = ShadePBR(albedo, N, V, mainLight.direction,
                                       mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation,
                                       _Metallic, _Smoothness);

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < lightCount; ++li)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    color += ShadePBR(albedo, N, V, l.direction,
                                      l.color * l.distanceAttenuation * l.shadowAttenuation,
                                      _Metallic, _Smoothness);
                }
                #endif

                // 4. Ambient — чтобы клинок не был чёрным в тени.
                half3 ambient = SampleSH(N);
                color += albedo * ambient * lerp(1.0, 0.45, _Metallic);

                // 5. Свечение: кромка + хамона + мерцание.
                half3 hot = HeatEmission(h);
                half hamon = HamonMask(i.uvRaw);
                half across = min(i.uvRaw.x, 1.0 - i.uvRaw.x);
                half edgeZone = 1.0 - smoothstep(0.0, 0.11, across);
                half tip = smoothstep(0.30, 1.0, i.uvRaw.y);
                half rim = pow(1.0 - saturate(dot(N, V)), _HeatRimPower);
                half flicker = 1.0 + (Fbm(float2(_Time.y * _FlickerSpeed, i.uvRaw.y * 11.0)) - 0.5)
                                     * 2.0 * _FlickerStrength * h;
                half emissiveMask = mask
                                  * (0.01 + hamon * _HamonBoost + edgeZone * 0.30)
                                  * (0.65 + rim * _HeatRimBoost)
                                  * (0.12 + 0.88 * h)
                                  * lerp(0.8, 1.2, tip);
                color += hot * emissiveMask * _HeatEmissive * max(flicker, 0.0);

                color = MixFog(color, i.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(ShadowAttributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            ShadowVaryings ShadowVert(ShadowAttributes v)
            {
                ShadowVaryings o;
                o.positionCS = GetShadowPositionHClip(v);
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ====================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes v)
            {
                DepthVaryings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 DepthFrag(DepthVaryings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
