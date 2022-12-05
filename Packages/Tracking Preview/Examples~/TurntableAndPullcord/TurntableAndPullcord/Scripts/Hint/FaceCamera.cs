/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Preview
{
    /// <summary>
    /// FaceCamera is used to update a transform, so that it always points in direction of the camera.
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        [SerializeField] private bool _lockZRotation = true;

        private float _initialZRot;

        void Start()
        {
            _initialZRot = transform.localEulerAngles.z;
        }

        // Update is called once per frame
        void Update()
        {
            transform.rotation = Quaternion.LookRotation(-Camera.main.transform.position + transform.position);

            if (_lockZRotation)
            {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, _initialZRot);
            }
        }
    }
}