using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicsHands
{
    public class NoContactParent : ContactParent
    {
        internal override void GenerateHands()
        {
            GenerateHandsObjects(typeof(NoContactHand));
        }

        internal override void PostFixedUpdateFrameLogic()
        {
        }
    }
}