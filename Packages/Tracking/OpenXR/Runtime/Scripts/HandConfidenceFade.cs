using Leap.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace Ultraleap.Tracking.OpenXR
{
    public class HandConfidenceFade : MonoBehaviour
    {
        private HandModelBase _handModel;
        private Renderer _renderer;
        private static readonly int ConfidenceFadeIndex = Shader.PropertyToID("_Confidence");
        private static readonly int PinchIndex = Shader.PropertyToID("_Pinch");

        void Awake()
        {
            _handModel = GetComponentInParent<HandModelBase>();
            if (_handModel == null)
            {
                Debug.LogError("Component does not have a HandModelBase");
                return;
            }

            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                Debug.LogError("Child component has no Renderer");
                return;
            }

            _handModel.OnUpdate += OnUpdate;
            _renderer.material.SetFloat(ConfidenceFadeIndex, 1.0f);
        }

        void OnUpdate()
        {
            var hand = _handModel.GetLeapHand();
            Vector3 thumbTipPosition = hand.GetThumb().TipPosition;
            Vector3 indexPinchPoint = (thumbTipPosition + hand.GetIndex().TipPosition) / 2.0f;
            Vector3 middlePinchPoint = (thumbTipPosition + hand.GetMiddle().TipPosition) / 2.0f;
            Vector3 ringPinchPoint = (thumbTipPosition + hand.GetRing().TipPosition) / 2.0f;
            Vector3 littlePinchPoint = (thumbTipPosition + hand.GetPinky().TipPosition) / 2.0f;
            
            //_renderer.material.SetFloat(ConfidenceFadeIndex, _handModel.GetLeapHand().Confidence);
            Vector4 indexPinch = indexPinchPoint;
            indexPinch.w = hand.PinchStrength;
            _renderer.material.SetVector(PinchIndex, indexPinch);
        }
    }
}