/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction
{

    public class SimpleFacingCameraCallbacks : MonoBehaviour
    {
        public bool IsFacingCamera { get; private set; }

        public Transform toFaceCamera;
        public Camera cameraToFace;

        private bool _initialized = false;

        public UnityEvent OnBeginFacingCamera;
        public UnityEvent OnEndFacingCamera;

        void Start()
        {
            if (toFaceCamera != null) initialize();
        }

        private void initialize()
        {
            if (cameraToFace == null) { cameraToFace = Camera.main; }
            // Set "_isFacingCamera" to be whatever the current state ISN'T, so that we are
            // guaranteed to fire a UnityEvent on the first initialized Update().
            IsFacingCamera = !GetIsFacingCamera(toFaceCamera, cameraToFace);
            _initialized = true;
        }

        void Update()
        {
            if (toFaceCamera != null && !_initialized)
            {
                initialize();
            }
            if (!_initialized) return;

            if (GetIsFacingCamera(toFaceCamera, cameraToFace, IsFacingCamera ? 0.77F : 0.82F) != IsFacingCamera)
            {
                IsFacingCamera = !IsFacingCamera;

                if (IsFacingCamera)
                {
                    OnBeginFacingCamera.Invoke();
                }
                else
                {
                    OnEndFacingCamera.Invoke();
                }
            }
        }

        public static bool GetIsFacingCamera(Transform facingTransform, Camera camera, float minAllowedDotProduct = 0.8F)
        {
            return Vector3.Dot((camera.transform.position - facingTransform.position).normalized, facingTransform.forward) > minAllowedDotProduct;
        }

    }

}