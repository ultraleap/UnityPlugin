/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

#if XR_UTILS_AVAILABLE
using Unity.XR.CoreUtils;
#endif

namespace Leap.Unity.Examples
{
    public class XRSceneHeadOrigin : MonoBehaviour
    {
        [SerializeField, Tooltip("If true, the cameraTransform will try to move to the sceneOrigin during startup")]
        private bool setOnStart = true;

        [SerializeField, Tooltip("Length of time that the cameraTransform will try to move to the sceneOrigin after startup (in Seconds)")]
        private float setOnStartLength = 2f;

        [SerializeField, Tooltip("Maximum distance the cameraTransform can be from the sceneOrigin during setOnStartLength after startup (in Meters)")]
        private float setOnStartDistance = 0.2f;

        [SerializeField, Tooltip("Should the Y rotation be set when starting up too?")]
        private bool includeRotationInStartup = false;

        [Header("Optional Transforms")]
        [SerializeField, Space, Tooltip("Where the camera should move to by default. If this is not set, the Transform that this XRSceneHeadOrigin component is attached to will be used")]
        private Transform sceneOrigin;

        [SerializeField, Space, Tooltip("Usually an XROrigin or CameraOffset. If this is not set, a suitable Transform will be found")]
        private Transform cameraOffsetOrigin;

        [SerializeField, Tooltip("The main camera being used. If this is not set, a suitable Transform will be found")]
        private Transform cameraTransform;

        void Start()
        {
            if (sceneOrigin == null)
            {
                sceneOrigin = transform;
            }

            // ensure a camera transform is set
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;
            }

#if XR_UTILS_AVAILABLE
            // Look for an XROrigin
            XROrigin xROrigin = FindAnyObjectByType<XROrigin>();

            if (xROrigin != null)
            {
                // use the XROrigin and exit out
                cameraOffsetOrigin = xROrigin.transform;
            }
            else
#endif
            {
                if (cameraTransform.parent != null)
                {
                    cameraOffsetOrigin = cameraTransform.parent;
                }
                else
                {
                    cameraOffsetOrigin = cameraTransform;
                }
            }

            if (setOnStart)
            {
                StartCoroutine(SetOriginDuringStartup());
            }
        }

        // Set the origin to this transform if the head becomes more than 20cm 
        IEnumerator SetOriginDuringStartup()
        {
            float time = 0;

            while (time < setOnStartLength)
            {
                yield return null;

                time += Time.deltaTime;

                if (Vector3.Distance(cameraTransform.position, sceneOrigin.position) > setOnStartDistance)
                {
                    SetHeadOrigin(includeRotationInStartup);
                }
            }
        }

        /// <summary>
        /// Reset head to the location that it was in when the scene loaded.
        /// </summary>
        /// <param name="includeRotation">Should this rotate the head to the original place as well?</param>
        public void SetHeadOrigin(bool includeRotation = false)
        {
            SetHeadOrigin(sceneOrigin, includeRotation);
        }

        /// <summary>
        /// Reset head to the target location.
        /// </summary>
        /// <param name="target">Where the heads position should be set to</param>
        /// <param name="includeRotation">Should this rotate the head to the original place as well?</param>
        public void SetHeadOrigin(Transform target, bool includeRotation = false)
        {

            if (includeRotation)
            {
                float rotationY = (target.transform.rotation * Quaternion.Inverse(cameraTransform.transform.rotation) * gameObject.transform.rotation).eulerAngles.y;
                cameraOffsetOrigin.transform.rotation = Quaternion.Euler(cameraOffsetOrigin.transform.eulerAngles.x, cameraOffsetOrigin.transform.eulerAngles.y + rotationY, cameraOffsetOrigin.transform.eulerAngles.z);
            }

            cameraOffsetOrigin.transform.position += target.position - cameraTransform.position;
        }

        /// <summary>
        /// Set height of the scene for the current user. Use this to adjust for height difference between users.
        /// </summary>
        public void SetHeight()
        {
            Vector3 heightOffset = sceneOrigin.position - cameraTransform.position;

            heightOffset.x = 0;
            heightOffset.z = 0;

            cameraOffsetOrigin.transform.position += heightOffset;
        }
    }
}