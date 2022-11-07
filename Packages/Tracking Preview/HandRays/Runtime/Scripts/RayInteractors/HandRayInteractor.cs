using System;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public abstract class HandRayInteractor : MonoBehaviour
    {
        public FarFieldLayerManager farFieldLayerManager;
        [SerializeField] private HandRay _handRay;
        public HandRay handRay => _handRay;

        [Tooltip("Note that the far field object layer will be added automatically from the FarFieldLayerManager")]
        [Header("Layer Logic")]
        public LayerMask layerMask = 0;
        public bool autoIncludeFloorLayer = false;

        [Header("Events")]
        public Action<RaycastHit[], RaycastHit> OnRaycastUpdate;

        [HideInInspector] public Vector3[] linePoints;
        [HideInInspector] public int numPoints;

        protected virtual void Start()
        {
            if (farFieldLayerManager == null)
            {
                farFieldLayerManager = FindObjectOfType<FarFieldLayerManager>();
            }

            layerMask |= farFieldLayerManager.FarFieldObjectLayer.layerMask;
            if (autoIncludeFloorLayer)
            {
                layerMask |= farFieldLayerManager.FloorLayer.layerMask;
            }
        }

        protected virtual void OnValidate()
        {
            if (farFieldLayerManager == null)
            {
                farFieldLayerManager = FindObjectOfType<FarFieldLayerManager>();
            }
        }

        protected virtual void OnEnable()
        {
            if (_handRay == null)
            {
                _handRay = FindObjectOfType<WristShoulderHandRay>();
                if (_handRay == null)
                {
                    Debug.LogWarning("HandRayParabolicLineRenderer needs a HandRay");
                    return;
                }
            }
            _handRay.OnHandRayFrame += UpdateRayInteractor;
        }

        protected virtual void OnDisable()
        {
            if (_handRay == null)
            {
                return;
            }

            _handRay.OnHandRayFrame -= UpdateRayInteractor;
        }

        protected virtual void UpdateRayInteractor(HandRayDirection handRayDirection)
        {
            UpdateRayInteractorLogic(handRayDirection, out RaycastHit[] allHits, out RaycastHit primaryHit);
            OnRaycastUpdate?.Invoke(allHits, primaryHit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handRayDirection"></param>
        /// <param name="allHits"></param>
        /// <param name="primaryHit"></param>
        /// <returns></returns>
        protected abstract int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] allHits, out RaycastHit primaryHit);
    }
}