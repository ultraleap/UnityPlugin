using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public abstract class ContactBone : MonoBehaviour
    {
        #region Bone Parameters
        internal int finger, joint;
        internal bool isPalm = false;
        public int Finger => finger;
        public int Bone => joint;
        public bool IsPalm => isPalm;

        internal float width, length;
        internal Vector3 tipPosition;
        #endregion

        #region Physics Data

        #endregion

        internal abstract void UpdatePalmBone(Hand hand);
        internal abstract void UpdateBone(Bone bone);

        internal abstract void PostFixedUpdateBone();
    }
}