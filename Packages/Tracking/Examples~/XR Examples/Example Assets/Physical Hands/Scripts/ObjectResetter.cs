/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;


namespace Leap.Unity.Examples
{
    public class ObjectResetter : MonoBehaviour
    {
        /// <summary>
        /// (metres) If this object moves this distance on the Y axis from where it started then we should reset it.
        /// </summary>
        public float distanceToReset = 2;

        Vector3 originalPos;
        Quaternion originalRot;

        Rigidbody[] rbs;
        Vector3[] rbPositions;
        Quaternion[] rbRotations;

        [Space, Tooltip("Event called when the object is reset to its starting position. Should be used to stop any scripts currently affecting this object.")]
        public UnityEvent OnReset;

        void Start()
        {
            originalPos = transform.position;
            originalRot = transform.rotation;
            rbs = GetComponentsInChildren<Rigidbody>();

            rbPositions = new Vector3[rbs.Length];
            rbRotations = new Quaternion[rbs.Length];

            for (int i = 0; i < rbs.Length; i++)
            {
                rbPositions[i] = rbs[i].position;
                rbRotations[i] = rbs[i].rotation;
            }
        }

        void FixedUpdate()
        {
            if (transform.position.y < originalPos.y - distanceToReset)
            {
                OnReset?.Invoke();

                if (rbs != null)
                {
                    for (int i = 0; i < rbs.Length; i++)
                    {
                        rbs[i].velocity = Vector3.zero;
                        rbs[i].angularVelocity = Vector3.zero;
                        rbs[i].MovePosition(rbPositions[i]);
                        rbs[i].MoveRotation(rbRotations[i]);
                    }
                }
                else
                {
                    transform.position = originalPos;
                    transform.rotation = originalRot;
                }
            }
        }
    }
}