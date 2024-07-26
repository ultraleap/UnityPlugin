/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap
{

    public class SimpleFacingCameraCallbacks : MonoBehaviour
    {
        public bool IsFacingCamera { get; private set; }

        public Transform toFaceCamera;
        public Camera cameraToFace;

        public UnityEvent OnBeginFacingCamera;
        public UnityEvent OnEndFacingCamera;

        void Start()
        {
            if (cameraToFace == null)
            {
                cameraToFace = Camera.main;
            }

            if (cameraToFace == null)
            {
                return;
            }

            IsFacingCamera = GetIsFacingCamera(toFaceCamera, cameraToFace);

            if (IsFacingCamera)
            {
                OnBeginFacingCamera?.Invoke();
            }
            else
            {
                OnEndFacingCamera?.Invoke();
            }
        }

        void Update()
        {
            if (cameraToFace == null)
            {
                cameraToFace = Camera.main;
            }

            if (cameraToFace == null)
            {
                return;
            }

            if (GetIsFacingCamera(toFaceCamera, cameraToFace, IsFacingCamera ? 0.77F : 0.82F) != IsFacingCamera)
            {
                IsFacingCamera = !IsFacingCamera; // state changed

                if (IsFacingCamera)
                {
                    OnBeginFacingCamera?.Invoke();
                }
                else
                {
                    OnEndFacingCamera?.Invoke();
                }
            }
        }

        public static bool GetIsFacingCamera(Transform facingTransform, Camera camera, float minAllowedDotProduct = 0.8F)
        {
            return Vector3.Dot((camera.transform.position - facingTransform.position).normalized, facingTransform.forward) > minAllowedDotProduct;
        }
    }
}