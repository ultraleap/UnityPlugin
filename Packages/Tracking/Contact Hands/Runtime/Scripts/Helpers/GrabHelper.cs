using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Leap.Unity.ContactHands
{
    [RequireComponent(typeof(ContactManager))]
    public class GrabHelper : MonoBehaviour
    {
        [Tooltip("If this is null then the Contact Manager's input provider will be used.")]
        public LeapProvider dataProvider;
        public ContactManager contactManager;

        private Collider[] _colliderCache = new Collider[10];

        private void OnEnable()
        {
            if (contactManager != null)
            {
                contactManager.OnFixedFrame += OnFixedFrame;
            }
        }

        private void OnDisable()
        {
            if(contactManager != null)
            {
                contactManager.OnFixedFrame -= OnFixedFrame;
            }
        }

        private void OnFixedFrame(Frame frame)
        {
            UpdateHandHeuristics(dataProvider == null ? contactManager.InputProvider.CurrentFixedFrame : dataProvider.CurrentFixedFrame, frame);
        }

        private void UpdateHandHeuristics(Frame dataFrame, Frame modifiedFrame)
        {

        }
        
        private void UpdateHandOverlaps()
        {

        }

        private void PalmOverlaps()
        {

        }

        private void JointOverlaps()
        {

        }

        // Unity 2022+
        private void PalmOverlapJobs()
        {

        }

        private void JointOverlapJobs()
        {

        }

        private void OnValidate()
        {
            if (contactManager == null)
            {
                contactManager = GetComponent<ContactManager>();
            }
        }
    }
}