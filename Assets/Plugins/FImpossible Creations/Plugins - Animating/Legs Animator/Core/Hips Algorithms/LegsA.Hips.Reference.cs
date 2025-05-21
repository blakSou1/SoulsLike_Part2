using FIMSpace.FTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public HipsReference HipsSetup = new HipsReference();

        [System.Serializable]
        public partial class HipsReference
        {
            public LegsAnimator Owner { get; private set; }

            [Tooltip("Applying elasticity algorithm on the pelvis bone align motion, to make it look more organic.")]
            [FPD_Suffix(0f, 1f)] public float HipsElasticityBlend = 1f;
            //[Range(0f, 1f)] public float HipsMotionInfluence = 1f;
            public FMuscle_Vector3 HipsMuscle;

            [FPD_Suffix(0f, 1f)] public float HipsRotElasticityBlend = 0f;
            public FMuscle_Quaternion HipsRotMuscle;

            [NonSerialized] public Vector3 LastKeyframePosition;
            [NonSerialized] public Vector3 LastKeyframeLocalPosition;
            [NonSerialized] public Quaternion LastKeyframeRotation;
            [NonSerialized] public Quaternion LastKeyframeLocalRotation;
            /// <summary> Keyframe, legs animator local space </summary>
            [NonSerialized] public Vector3 LastRootLocalPos;

            public List<Leg> ChildLegs { get; private set; }

            /// <summary> Its one when hips is in the same height as in initial pose, zero when hips are in zero local position </summary>
            [NonSerialized] public float LastHipsHeightDiff;

            [NonSerialized] public Vector3 InitHipsPositionRootSpace;
            [NonSerialized] public float InitialHipsHeightLocal;
            [NonSerialized] internal Quaternion _LastHipsRotationOffsetOutsideInfo = Quaternion.identity;

            Transform root;
            public Transform Bone { get; private set; }
            private Vector3 initLocalPos;
            private Quaternion initLocalRot;

            public UniRotateBone UniRotate { get; private set; }


            #region Setup


            public void Initialize(LegsAnimator owner, Transform bone, Transform root)
            {
                Owner = owner;
                this.Bone = bone;
                this.root = root;
                initLocalPos = bone.localPosition;
                initLocalRot = bone.localRotation;
                
                ExtraNonElasticOffset = Vector3.zero;
                _Hips_StabilityLocalAdjustement = Vector3.zero;
                _Hips_sd_StabilAdjustm = Vector3.zero;

                InitHipsPositionRootSpace = root.InverseTransformPoint(bone.position);
                InitialHipsHeightLocal = InitHipsPositionRootSpace.y;

                HipsMuscle ??= new FMuscle_Vector3();
                HipsRotMuscle ??= new FMuscle_Quaternion();

                HipsMuscle.Initialize(Vector3.zero);
                HipsRotMuscle.Initialize(Quaternion.identity);

                UniRotate = new UniRotateBone(bone, root);
                Calibrate();
            }


            internal void PrepareLegs()
            {
                ChildLegs = new List<Leg>();
                if (Owner._hipsHubs_using == false) ChildLegs = Owner.Legs;
                else
                {
                    // Individual child legs for leg hubs
                    for (int l = 0; l < Owner.Legs.Count; l++)
                    {
                        bool? isPar = IsFirstParent(Owner.Legs[l], Bone);

                        if (isPar == true)
                            ChildLegs.Add(Owner.Legs[l]);
                        else if (isPar == null) // Not found any hub to be the parent - add to the main hub
                        {
                            if ( this == Owner.HipsSetup)
                                ChildLegs.Add(Owner.Legs[l]);
                        }
                    }
                }

                for (int l = 0; l < ChildLegs.Count; l++)
                    ChildLegs[l].AssignParentHub(this);
            }


            public class HipsHubBackbone
            {
                public LegsAnimator Owner { get; private set; }
                public Transform Bone { get; private set; }
                public Quaternion InitialLocalRotation { get; private set; }
                public Vector3 KeyframePosition { get; private set; }
                public Transform frontBone;

                public Quaternion TargetRotation { get; internal set; }

                public HipsHubBackbone(LegsAnimator owner, Transform b)
                {
                    Owner = owner;
                    Bone = b;
                    InitialLocalRotation = b.localRotation;
                    _FMuscle = new FMuscle_Vector3();
                    _FMuscle.Initialize(Vector3.zero);
                }

                public void PreCalibrate()
                {
                    Bone.localRotation = InitialLocalRotation;
                }

                public void Calibrate()
                {
                    KeyframePosition = Bone.position;
                }

                Vector3 _dir = Vector3.zero;
                Vector3 _sd_dir = Vector3.zero;
                FMuscle_Vector3 _FMuscle;
                public Vector3 AnimateTargetDirection(Vector3 toHubNewB)
                {
                    if (Owner.HubBackBonesElasticity < 0.0001f) return toHubNewB;
                    else
                    {
                        if ( Owner.HubBackBonesElasticity <= 0.1f)
                            _dir = Vector3.SmoothDamp(_dir, toHubNewB, ref _sd_dir, 0.001f + Owner.HubBackBonesElasticity, 10000000f, Owner.DeltaTime);
                        else
                            _dir = Vector3.LerpUnclamped(toHubNewB, _FMuscle.Update(Owner.DeltaTime, toHubNewB), Owner.HubBackBonesElasticity);
                    }

                    return _dir;
                }
            }

            public List<HipsHubBackbone> HubBackBones { get; private set; }
            internal void PrepareHubBones()
            {
                PrepareLegs();
                HubBackBones = new List<HipsHubBackbone>();

                Transform preBone = Bone;
                Transform parent = Bone.parent;
                while (parent != null)
                {
                    bool hardBreak = false;
                    for (int o = 0; o < Owner.HipsHubs.Count; o++)
                        if (parent == Owner.HipsHubs[o].Bone) { hardBreak = true; break; }

                    if (hardBreak) break;

                    HipsHubBackbone bBone = new HipsHubBackbone(Owner, parent);
                    bBone.frontBone = preBone;
                    HubBackBones.Add(bBone);

                    if (parent == Owner.HipsSetup.Bone) break;

                    preBone = parent;
                    parent = parent.parent;
                }
            }


            bool? IsFirstParent(Leg leg, Transform hub)
            {
                if ( leg.BoneStart == null ) return false;
                Transform t = leg.BoneStart;

                while(t != null)
                {
                    if (t == hub) return true;
                    else
                    {
                        if (t == Owner.Hips) return false;
                        for (int i = 0; i < Owner.ExtraHipsHubs.Count; i++)
                            if (t == Owner.ExtraHipsHubs[i]) return false;
                    }

                    t = t.parent;
                }

                return null;
            }


            public void Reset()
            {
                Calibrate();
                Hips_LastHipsOffset = 0f;
            }

            public void PreCalibrate()
            {
                UniRotate.PreCalibrate();

                //bone.localPosition = initLocalPos;
                //bone.localRotation = initLocalRot;

                if( Owner.Calibrate != ECalibrateMode.FixedCalibrate )
                    UniRotate.PreCalibrate();
                else
                {
                    Bone.SetLocalPositionAndRotation(LastKeyframeLocalPosition, LastKeyframeLocalRotation);
                }

                if ( HubBackBones != null) for (int h = 0; h < HubBackBones.Count; h++) HubBackBones[h].PreCalibrate();
            }

            public void Calibrate()
            {
                LastKeyframePosition = Bone.position;
                LastKeyframeLocalPosition = Bone.localPosition;
                LastKeyframeLocalRotation = Bone.localRotation;
                LastKeyframeRotation = Bone.rotation;
                LastRootLocalPos = Owner.ToRootLocalSpace(LastKeyframePosition);
                LastHipsHeightDiff = GetHeightDiff(LastRootLocalPos.y);
                if (HubBackBones != null) for (int h = 0; h<HubBackBones.Count; h++) HubBackBones[h].Calibrate();
            }

            /// <summary> Its one when rootSpaceHeight is in the same height as in initial pose hips height, zero when rootSpaceHeight is in zero local position height </summary>
            public float GetHeightDiff(float rootSpaceHeight)
            {
                return Mathf.InverseLerp(0f, InitialHipsHeightLocal, rootSpaceHeight);
            }

            #endregion


            public void CopyMuscleSettingsFrom(HipsReference hipsSetup)
            {
                HipsMuscle.Acceleration = hipsSetup.HipsMuscle.Acceleration;
                HipsMuscle.AccelerationLimit = hipsSetup.HipsMuscle.AccelerationLimit;
                HipsMuscle.Damping = hipsSetup.HipsMuscle.Damping;
                HipsMuscle.BrakePower = hipsSetup.HipsMuscle.BrakePower;
            }

        }

    }
}