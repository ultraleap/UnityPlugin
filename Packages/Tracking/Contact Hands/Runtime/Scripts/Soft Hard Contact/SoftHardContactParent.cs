using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.ContactHands
{
    public class SoftHardContactParent : HardContactParent
    {
        internal override void GenerateHands()
        {
            _physicsMaterial = CreateHandPhysicsMaterial();
            GenerateHandsObjects(typeof(SoftHardContactHand));
        }
    }
}