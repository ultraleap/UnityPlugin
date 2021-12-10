/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

namespace Leap.Unity.Interaction
{

    /// <summary>
    /// Implements IVRControllerTrackingProvider using Unity.XR.InputTracking for XRNodes.
    /// This tracking should support all native XR controller integrations in Unity,
    /// including Oculus Touch and HTC Vive.
    /// </summary>
    public class DefaultXRNodeTrackingProvider : MonoBehaviour,
                                                 IXRControllerTrackingProvider
    {

        private bool _isTrackingController = false;
        public bool isTracked { get { return _isTrackingController; } }

        private bool _isXRNodeSet = false;
#if UNITY_2017_2_OR_NEWER
        private XRNode _backingXRNode;
        public XRNode xrNode
        {
            get { return _backingXRNode; }
            set { _backingXRNode = value; _isXRNodeSet = true; }
        }
#else
        private VRNode _backingXRNode;
        public VRNode xrNode
        {
            get { return _backingXRNode; }
            set { _backingXRNode = value; _isXRNodeSet = true; }
        }
#endif

        public event Action<Vector3, Quaternion> OnTrackingDataUpdate = (position, rotation) => { };

        void FixedUpdate()
        {
            updateTrackingData();
        }

        void updateTrackingData()
        {
            if (_isXRNodeSet)
            {

                var position = XRSupportUtil.GetXRNodeLocalPosition((int)xrNode);
                var rotation = XRSupportUtil.GetXRNodeLocalRotation((int)xrNode);

                // Unfortunately, the only alternative to checking the controller's position and
                // rotation for whether or not it is tracked is to request an allocated string
                // array of all currently-connected joysticks, which would allocate garbage
                // every frame, so it's unusable.
                _isTrackingController = position != Vector3.zero && rotation != Quaternion.identity;

                Transform rigTransform = MainCameraProvider.mainCamera.transform.parent;
                if (rigTransform != null)
                {
                    position = rigTransform.TransformPoint(position);
                    rotation = rigTransform.TransformRotation(rotation);
                }

                OnTrackingDataUpdate(position, rotation);
            }
        }

    }

}