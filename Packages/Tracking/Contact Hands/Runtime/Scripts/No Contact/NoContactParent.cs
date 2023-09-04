using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class NoContactParent : ContactParent
    {
        internal override void GenerateHands()
        {
            GenerateHandsObjects(typeof(NoContactHand));
        }

        internal override void PostFixedUpdateFrame()
        {
        }
    }
}