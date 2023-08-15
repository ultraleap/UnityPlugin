using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{

    public class ContactManager : PostProcessProvider
    {
        public ContactParent contactHands;

        private WaitForFixedUpdate _postFixedUpdateWait = null;

        internal Leap.Hand _leftDataHand = new Hand(), _rightDataHand = new Hand();
        internal int _leftHandIndex, _rightHandIndex;

        private void Awake()
        {
            contactHands = GetComponentInChildren<ContactParent>(true);
        }

        public override void ProcessFrame(ref Frame inputFrame)
        {
            _leftHandIndex = inputFrame.Hands.FindIndex(x => x.IsLeft);
            if (_leftHandIndex != -1)
            {
                _leftDataHand.CopyFrom(inputFrame.Hands[_leftHandIndex]);
            }

            _rightHandIndex = inputFrame.Hands.FindIndex(x => x.IsRight);
            if (_rightHandIndex != -1)
            {
                _rightDataHand.CopyFrom(inputFrame.Hands[_rightHandIndex]);
            }

            contactHands?.UpdateFrame();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            StartCoroutine(PostFixedUpdate());
        }

        private IEnumerator PostFixedUpdate()
        {
            if(_postFixedUpdateWait == null)
            {
                _postFixedUpdateWait = new WaitForFixedUpdate();
            }
            for(; ; )
            {
                contactHands?.PostFixedUpdateFrame();
                yield return _postFixedUpdateWait;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            dataUpdateMode = DataUpdateMode.UpdateAndFixedUpdate;
        }

    }

}