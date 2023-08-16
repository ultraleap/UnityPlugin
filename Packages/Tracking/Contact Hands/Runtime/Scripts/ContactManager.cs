using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class ContactManager : LeapProvider
    {

        [SerializeField] private LeapProvider _inputProvider;

        public ContactParent contactHands;

        private WaitForFixedUpdate _postFixedUpdateWait = null;

        internal Leap.Hand _leftDataHand = new Hand(), _rightDataHand = new Hand();
        internal int _leftHandIndex, _rightHandIndex;

        private Frame _modifiedFrame = new Frame();

        public override Frame CurrentFrame => _modifiedFrame;

        public override Frame CurrentFixedFrame => _modifiedFrame;

        private void Awake()
        {
            contactHands = GetComponentInChildren<ContactParent>(true);
        }

        private void ProcessFrame(Frame inputFrame)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _modifiedFrame.CopyFrom(inputFrame);

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

            // Output the frame on update
            if (!Time.inFixedTimeStep)
            {
                contactHands?.OutputFrame(ref _modifiedFrame);
                DispatchUpdateFrameEvent(_modifiedFrame);
            }
        }

        private void OnEnable()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnUpdateFrame -= ProcessFrame;
                _inputProvider.OnUpdateFrame += ProcessFrame;
                _inputProvider.OnFixedFrame -= ProcessFrame;
                _inputProvider.OnFixedFrame += ProcessFrame;

                StartCoroutine(PostFixedUpdate());
            }
        }

        private void OnDisable()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnUpdateFrame -= ProcessFrame;
                _inputProvider.OnFixedFrame -= ProcessFrame;
            }
        }

        private IEnumerator PostFixedUpdate()
        {
            if (_postFixedUpdateWait == null)
            {
                _postFixedUpdateWait = new WaitForFixedUpdate();
            }
            // Need to wait one frame to prevent early execution
            yield return null;
            for (; ; )
            {
                contactHands?.PostFixedUpdateFrame();
                // Output the frame after physics update is complete
                contactHands?.OutputFrame(ref _modifiedFrame);
                DispatchFixedFrameEvent(_modifiedFrame);
                yield return _postFixedUpdateWait;
            }
        }

        private void OnValidate()
        {
            if (contactHands == null)
            {
                contactHands = GetComponentInChildren<ContactParent>(true);
            }
        }
    }

}