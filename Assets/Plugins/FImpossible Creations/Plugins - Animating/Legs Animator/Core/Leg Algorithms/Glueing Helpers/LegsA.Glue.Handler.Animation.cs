using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            partial class GlueAttachementHandler
            {
                public LegTransitionAnimation legMoveAnimation { get; private set; }

                /// <summary>
                /// Class responsitve for transitioning IK target position between two positions.
                /// Animation mode is doign simple "move towards" transition
                /// but Idle Mode is animating leg ik with use of space curves.
                /// </summary>
                public class LegTransitionAnimation
                {
                    private GlueAttachementHandler handler;
                    LegsAnimator Owner { get { return handler.Owner; } }
                    Leg leg { get { return handler.Leg; } }

                    #region Leg Adjust Animation Parameters

                    public float LegAdjustementYOffset = 0f; // Leg movement from to, y mod
                    public float LegAdjustementFootAngleOffset = 0f; // Leg movement foot pitch angle extra animation
                    Vector3 _legSpherizeLocalVector = Vector3.zero; // Leg movement from to, z mod
                    float _legMoveDurMul = 1f;
                    Quaternion baseRotationOnStepUp;
                    public float legMoveDistanceFactor = 0f;

                    float sd_trProgress = 0f;

                    #endregion


                    public bool DuringLegAdjustMovement { get; private set; }
                    public bool WasAttaching { get; private set; }
                    public bool Attached { get; private set; }
                    public float TransitionProgress { get; private set; }
                    public float LastAttachCompleteTime { get; private set; }
                    public float TransitionProgressLastFrame { get; private set; }

                    Vector3 previousPositionLocal;
                    Vector3 previousPositionWorld;
                    Quaternion previousRotationWorld;

                    Vector3 lastAppliedGluePosition;
                    Vector3 lastAppliedGluePositionLocal;
                    Quaternion lastAppliedGlueRotation;
                    float lastSpeedup = 0f;

                    enum EMoveType { FromAnimation, FromLastAttachement }
                    EMoveType animationMoveType;

                    public EGlueMode LastAnimationGlueMode { get { return ( animationMoveType == EMoveType.FromAnimation ) ? EGlueMode.Moving : EGlueMode.Idle; } }


                    public LegTransitionAnimation( GlueAttachementHandler glueTransitionHelper )
                    {
                        handler = glueTransitionHelper;

                        Reset();
                    }

                    public void Reset()
                    {
                        animationMoveType = EMoveType.FromAnimation;
                        TransitionProgress = 0f;
                        TransitionProgressLastFrame = 0f;
                        baseRotationOnStepUp = Owner.BaseTransform.rotation;

                        DuringLegAdjustMovement = false;
                        WasAttaching = false;
                        Attached = false;
                        _legSpherizeLocalVector = Vector3.zero;

                        ReInitialize();
                    }

                    public void ReInitialize()
                    {
                        lastAppliedGluePosition = leg._SourceIKPos;
                        lastAppliedGlueRotation = leg._SourceIKRot;

                        previousPositionWorld = leg._SourceIKPos;
                        previousRotationWorld = leg._SourceIKRot;
                        previousPositionLocal = leg.ToRootLocalSpace( leg._SourceIKPos );
                    }


                    #region Instant Transition

                    bool _instantTransition = false;
                    internal void ScheduleInstantTransition()
                    {
                        _instantTransition = true;
                    }

                    #endregion


                    internal void DoAttaching( bool canAttach )
                    {
                        if( canAttach != WasAttaching )
                        {
                            WasAttaching = canAttach;

                            if( canAttach )
                                OnChangeTargetPosition();
                            else
                            {
                                Attached = false;
                                if( TransitionProgress != 0f ) OnChangeTargetPosition();
                            }
                        }

                        if( DuringLegAdjustMovement )
                        {
                            if( TransitionProgress >= 1f )
                                DuringLegAdjustMovement = false;
                        }
                    }


                    bool _wasAnimatingLeg = false;

                    /// <summary>
                    /// Ensure that current leg height is above ground level (preventing floor clipping on animation transition)
                    /// </summary>
                    internal Vector3 EnsureAnkleNotOverlappingGroundLevel( Vector3 legAnimPos )
                    {
                        if( leg.A_PreWasAligning && leg.A_WasAligningFrameBack )
                        {
                            Vector3 animPosLocal = Owner.ToRootLocalSpace( legAnimPos );

                            Vector3 refLocal;
                            if( Owner.SmoothSuddenSteps < 0.0001f )
                                refLocal = leg.ankleAlignedOnGroundHitRootLocal;
                            else
                                refLocal =  leg.A_WasSmoothing  ? leg.A_LastSmoothTargetedPosLocal : leg.ankleAlignedOnGroundHitRootLocal;

                            if( animPosLocal.y < refLocal.y )
                            {
                                animPosLocal.y = refLocal.y;
                                //UnityEngine.Debug.Log("Old Pos = " + legAnimPos + " new Pos = " + (Owner.RootToWorldSpace(animPosLocal)));
                                //UnityEngine.Debug.DrawLine(legAnimPos, (Owner.RootToWorldSpace(animPosLocal)), Color.green, 1.01f);
                                legAnimPos = Owner.RootToWorldSpace( animPosLocal );
                            }
                        }

                        return legAnimPos;
                    }

                    /// <summary> Idle Gluing Leg Animation </summary>
                    public Vector3 CalculateAnimatedLegPosition( Vector3 a, Vector3 b )
                    {
                        var sett = leg.LegAnimatingSettings;
                        Vector3 legAnimPos = Vector3.LerpUnclamped( a, b, sett.MoveToGoalCurve.Evaluate( TransitionProgress ) );

                        // Spherize side offset animation compute
                        if( sett.SpherizeTrack.length > 1 )
                        {
                            float transitEval = sett.SpherizeTrack.Evaluate( TransitionProgress ) * sett.SpherizePower * Owner.BaseTransform.lossyScale.x;

                            // Limit spherize offset
                            legAnimPos += leg.RootSpaceToWorldVec( _legSpherizeLocalVector * ( transitEval * 12f ) );
                        }

                        // Feet animation info value compute
                        if( Owner.AnimateFeet )
                        {
                            LegAdjustementFootAngleOffset = sett.FootRotationCurve.Evaluate( TransitionProgress ) * 90f * Mathf.Min( 0.5f, legMoveDistanceFactor * 1.1f );
                            LegAdjustementFootAngleOffset /= lastSpeedup;
                        }

                        // Prepare foot height offset value
                        float scaleRef = Owner.ScaleReferenceNoScale * 0.75f;
                        float height = Mathf.Lerp( sett.MinFootRaise, sett.MaxFootRaise, legMoveDistanceFactor );
                        height *= scaleRef;

                        LegAdjustementYOffset = height * sett.RaiseYAxisCurve.Evaluate( TransitionProgress );
                        _wasAnimatingLeg = true;

                        return legAnimPos;
                    }

                    /// <summary> Compute target position for the next glue attachement </summary>
                    internal Vector3 GetTargetPosition()
                    {
                        float attachBlend = handler.glueAnimationBlend;

                        if( animationMoveType == EMoveType.FromAnimation ) // From animation to attachement
                        {
                            if( attachBlend < 0.0001f ) return Owner.RootToWorldSpace( previousPositionLocal );

                            Vector3 a = Owner.RootToWorldSpace( previousPositionLocal );
                            if( TransitionProgress < 0.0001f ) return a;

                            Vector3 b;
                            if( Attached ) // fading from last glue
                            {
                                if( attachBlend > 0.9995f )
                                    b = leg._GlueLastAttachPosition;
                                else
                                {
                                    if( leg.Owner.OnlyLocalAnimation )
                                    { b = leg.RootSpaceToWorld( leg._GlueLastAttachPositionRootLocal ); }
                                    else
                                        b = Vector3.LerpUnclamped( leg.RootSpaceToWorld( leg._GlueLastAttachPositionRootLocal ), leg._GlueLastAttachPosition, attachBlend );
                                }
                            }
                            else // Pinning towards grounded position
                                b = leg.AnkleAlignedOnGroundHitWorldPos;

                            if( TransitionProgress > .9995f ) return b;
                            else return Vector3.LerpUnclamped( a, b, TransitionProgress );
                        }
                        else // From attachement to attachement
                        {
                            Vector3 a;
                            if( leg.Owner.OnlyLocalAnimation )
                            {
                                a = Owner.RootToWorldSpace( previousPositionLocal );
                                if( TransitionProgress < 0.0001f ) return a;
                            }
                            else
                            {
                                a = previousPositionWorld;
                                if( TransitionProgress < 0.0001f ) return a;

                                // From world to local initial point to compensate dynamic character aligning
                                a = Vector3.LerpUnclamped( previousPositionWorld, Owner.RootToWorldSpace( previousPositionLocal ), TransitionProgress );
                            }

                            Vector3 b;
                            if( TransitionProgress > 0.9995f ) b = leg._GlueLastAttachPosition;
                            else b = CalculateAnimatedLegPosition( a, leg.AnkleAlignedOnGroundHitWorldPos );

                            if( TransitionProgress >= 1f )
                                return b;
                            else
                            {
                                float om = 1f - TransitionProgress;
                                b = Vector3.LerpUnclamped( a, b, 1f - ( om * om ) );
                                return b;
                            }
                        }
                    }

                    internal void RequireRepose()
                    {
                        if( Attached )
                        {
                            Attached = false;
                            OnChangeTargetPosition();
                        }
                    }

                    internal Quaternion GetTargetRotation()
                    {
                        Quaternion a = previousRotationWorld;
                        Quaternion finRot;

                        if( TransitionProgress < 0.001f )
                        {
                            finRot = a;
                            return finRot;
                        }

                        Quaternion b;

                        if( Attached ) // fading from last glue
                            b = leg._GlueLastAttachRotation;
                        else // Pinning towards grounded rotation
                            b = leg.ankleAlignedOnGroundHitRotation; // IMPORTANT


                        if( TransitionProgress > .9995f )
                            finRot = b;
                        else
                            finRot = Quaternion.LerpUnclamped( a, b, TransitionProgress );

                        return finRot;
                    }


                    internal void OnChangeTargetPosition()
                    {
                        handler.LasGlueModeOnAttaching = Owner._glueModeExecuted;
                        baseRotationOnStepUp = Owner.BaseTransform.rotation;

                        #region Determinate type of gluing animation to execute on change

                        if( handler.glueAnimationBlend < 0.2f )
                            animationMoveType = EMoveType.FromAnimation;
                        else
                        {
                            if( handler.LasGlueModeOnAttaching == EGlueMode.Moving )
                                animationMoveType = EMoveType.FromAnimation;
                            else
                            {
                                if( animationMoveType == EMoveType.FromLastAttachement )
                                    animationMoveType = EMoveType.FromLastAttachement;
                                else
                                {
                                    if( handler.glueAnimationBlend > 0.75f )
                                    {
                                        if( TransitionProgress < 0.1f || TransitionProgress > 0.9f )
                                            animationMoveType = EMoveType.FromLastAttachement;
                                        else
                                            animationMoveType = EMoveType.FromAnimation;
                                    }
                                    else
                                        animationMoveType = EMoveType.FromAnimation;
                                }
                            }
                        }

                        #endregion

                        if( leg.Owner.OnlyLocalAnimation )
                            previousPositionWorld = leg.RootSpaceToWorld( lastAppliedGluePositionLocal );
                        else
                            previousPositionWorld = lastAppliedGluePosition;

                        previousRotationWorld = lastAppliedGlueRotation;
                        previousPositionLocal = Owner.ToRootLocalSpace( previousPositionWorld );

                        #region Computing idle gluing leg animation parameters

                        if( animationMoveType == EMoveType.FromLastAttachement )
                        {
                            if( TransitionProgress > 0.1f && TransitionProgress < 0.9f ) // Break currently executed transitioning
                            {
                                //UnityEngine.Debug.Log("break");
                                //breakIdleGlueTime = Time.time;
                                //previousBreakLocal = Owner.ToRootLocalSpace(leg._PreviousFinalIKPos);
                                //transitionProgress = 1f;
                            }
                            else // Transitioning start over
                            {
                                TransitionProgress = 0f;
                            }

                            Vector3 from = previousPositionWorld;
                            Vector3 to = leg.AnkleAlignedOnGroundHitWorldPos;
                            Vector3 diff = to - from;

                            float fromToDistance = diff.magnitude;
                            legMoveDistanceFactor = ( fromToDistance ) / ( Owner.ScaleReference * 0.6f );
                            legMoveDistanceFactor = Mathf.Clamp( legMoveDistanceFactor, 0.05f, 1f );

                            Vector3 towards = diff.normalized;
                            towards = Vector3.ProjectOnPlane( towards, Owner.Up );
                            towards.Normalize();

                            leg.SendRaiseEvent( fromToDistance );

                            if( legMoveDistanceFactor > 0.0401f )
                            {
                                _legMoveDurMul = Mathf.Lerp( 1.55f, .85f, legMoveDistanceFactor * 2f );

                                Vector3 cross = Vector3.Cross( towards, Owner.Up );
                                cross.Normalize();

                                _legSpherizeLocalVector = leg.ToRootLocalSpaceDir( cross ) * Owner.ScaleReferenceNoScale * -0.03f;

                                DuringLegAdjustMovement = true;
                            }
                            else // If step distance if very small, skip leg move animation and slide foots towards target position in a subtle way
                            {
                                animationMoveType = EMoveType.FromAnimation;
                                _legSpherizeLocalVector = Vector3.zero;
                                DuringLegAdjustMovement = false;
                            }

                        }
                        else
                        {
                            DuringLegAdjustMovement = false;
                            TransitionProgress = 0f;
                        }

                        #endregion
                    }

                    public void UpdateAnimation()
                    {
                        float boostSD = ( Owner.JustGrounded ) ? 0.2f : 1f;
                        float boostLrp = ( Owner.JustGrounded ) ? 5f : 1f;

                        TransitionProgressLastFrame = TransitionProgress;

                        if( _instantTransition )
                        {
                            _instantTransition = false;
                            TransitionProgress = 1f;
                            LastAttachCompleteTime = Time.time;
                        }

                        if( !Owner.IsGrounded ) return;

                        if( animationMoveType == EMoveType.FromLastAttachement )
                        {
                            float animTime = 1f / ( leg.LegAnimatingSettings.StepMoveDuration * 0.8f );

                            #region Speedups

                            float speedup = 1f;
                            lastSpeedup = 1f;

                            if( leg.LegAnimatingSettings.AllowSpeedups > 0f )
                            {

                                if( leg.hasOppositeleg )
                                {
                                    var oppositeleg = leg.GetOppositeLeg();

                                    Vector3 prePos = oppositeleg._PreviousFinalIKPos;
                                    if( leg.Owner.OnlyLocalAnimation ) prePos = leg.RootSpaceToWorld( oppositeleg._PreviousFinalIKPosRootLocal );

                                    float stretch = oppositeleg.IKProcessor.GetStretchValue( prePos );
                                    if( stretch > leg.LegStretchLimit * 0.95f )
                                    {
                                        float diff = ( stretch - leg.LegStretchLimit * 0.95f ) * 2.0f;
                                        if( diff < 0f ) diff = 0f;
                                        speedup += diff;
                                    }

                                    if( oppositeleg._UsingCustomRaycast == false )
                                        if( oppositeleg.G_AttachementHandler.legMoveAnimation.Attached )
                                        {
                                            float distToAttach = ( leg.RootSpaceToWorld( oppositeleg.AnkleH.LastKeyframeRootPos ) - oppositeleg.G_Attachement.GetRelevantHitPoint() ).magnitude;
                                            float scaleRef = Owner.ScaleReference * 0.4f;
                                            if( distToAttach > scaleRef )
                                            {
                                                float diff = distToAttach - scaleRef;
                                                speedup +=  diff / scaleRef  * 2f;
                                            }
                                        }
                                }

                                if( leg.LegAnimatingSettings.AllowSpeedups > 0.25f )
                                {
                                    float diff = Quaternion.Angle( baseRotationOnStepUp, Owner.BaseTransform.rotation );
                                    if( diff > 12f )
                                    {
                                        float angularFactor = Mathf.InverseLerp( 30f, 135f, diff );
                                        angularFactor = Mathf.LerpUnclamped( 0.5f, 2f, angularFactor ) * ( 0.4f + leg.LegAnimatingSettings.AllowSpeedups * 0.6f );
                                        TransitionProgress += Owner.DeltaTime * angularFactor * boostLrp;
                                    }
                                }

                                speedup = Mathf.LerpUnclamped( 1f, speedup, leg.LegAnimatingSettings.AllowSpeedups );
                            }

                            lastSpeedup = speedup;

                            #endregion

                            TransitionProgress = Mathf.MoveTowards( TransitionProgress, 1f, animTime * speedup * _legMoveDurMul * leg.LegMoveSpeedMultiplier * Owner.DeltaTime * boostLrp );

                            if( TransitionProgress > .9995f )
                            {
                                if( DuringLegAdjustMovement )
                                    TriggerAttach();
                            }

                            return;
                        }

                        if( TransitionProgress > .9995f && handler.glueAnimationBlend > 0.95f )
                            TriggerAttach();
                        else
                            TransitionProgress = Mathf.SmoothDamp( TransitionProgress, 1.001f, ref sd_trProgress, ( 0.01f + Mathf.LerpUnclamped( 0.225f, 0.01f, WasAttaching ? Owner.GlueFadeInSpeed : Owner.GlueFadeOutSpeed ) ) * boostSD, 10000000f, Owner.DeltaTime );
                    }

                    void TriggerAttach()
                    {
                        if( !Attached )
                        {
                            TransitionProgress = 1f;
                            LastAttachCompleteTime = Time.time;
                            Attached = leg.Glue_TriggerFinalAttach();
                            DuringLegAdjustMovement = false;
                        }
                    }

                    public void PostUpdate()
                    {
                        lastAppliedGluePosition = leg._GluePosition;
                        lastAppliedGluePositionLocal = leg.ToRootLocalSpace( lastAppliedGluePosition );
                        lastAppliedGlueRotation = leg._GlueRotation;

                        if( _wasAnimatingLeg == false ) // Fade off in case of broken transition animation
                        {
                            LegAdjustementFootAngleOffset = Mathf.MoveTowards( LegAdjustementFootAngleOffset, 0f, leg.DeltaTime * 20f );
                            LegAdjustementYOffset = Mathf.MoveTowards( LegAdjustementYOffset, 0f, leg.DeltaTime * 20f );
                        }
                        else
                            _wasAnimatingLeg = false;
                    }

                }
            }

            GlueAttachementHandler.LegTransitionAnimation G_LegAnimation { get { return G_AttachementHandler.legMoveAnimation; } }
        }
    }
}