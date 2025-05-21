using UnityEngine;

namespace FIMSpace.FProceduralAnimation
{
    public partial class LegsAnimator
    {
        public partial class Leg
        {
            public Vector3 InitialPosInRootSpace { get; private set; }
            void Stability_Init()
            {
                Vector3 initRootSpacePos = ToRootLocalSpace(BoneEnd.position);
                InitialPosInRootSpace = initRootSpacePos;
            }
        }
    }
}