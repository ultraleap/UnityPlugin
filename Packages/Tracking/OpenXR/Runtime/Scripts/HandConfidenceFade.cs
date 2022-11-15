using Leap.Unity;
using UnityEngine;

namespace Ultraleap.Tracking.OpenXR
{
    public class HandConfidenceFade : MonoBehaviour
    {
        private HandModelBase _handModel;
        private Renderer _renderer;
        private static readonly int ConfidenceFadeIndex = Shader.PropertyToID("_Confidence");

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
            _renderer.material.SetFloat(ConfidenceFadeIndex, _handModel.GetLeapHand().Confidence);
        }
    }
}