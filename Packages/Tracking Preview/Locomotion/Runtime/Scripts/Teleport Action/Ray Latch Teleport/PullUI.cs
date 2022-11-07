/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    /// <summary>
    /// Controls a sphere & line renderer UI to display progress along it
    /// </summary>
    public class PullUI : MonoBehaviour
    {
        [SerializeField] private GameObject _sphere;
        [SerializeField] private GameObject _sphereTarget;
        [SerializeField] private LineRenderer _line;
        [SerializeField] private LineRenderer _lineTarget;

        [SerializeField] private float _pinchOffset = 0.01f;

        /// <summary>
        /// Sets the current progress of PullUI
        /// </summary>
        /// <param name="currentLength">The current length the UI has moved</param>
        /// <param name="targetLength">The target length for the UI to move</param>
        /// <param name="direction">The direction the UI is pointing in</param>
        public void SetProgress(float currentLength, float targetLength, Vector3 direction)
        {
            _line.SetPosition(0, transform.position - new Vector3(0, _pinchOffset, 0));
            _line.SetPosition(1, transform.position + (direction * currentLength) - new Vector3(0, _pinchOffset, 0));

            _sphere.transform.position = transform.position + (direction * currentLength) - new Vector3(0, _pinchOffset, 0);

            _lineTarget.SetPosition(0, transform.position + (direction * currentLength) - new Vector3(0, _pinchOffset, 0));
            _lineTarget.SetPosition(1, transform.position + (direction * targetLength) - new Vector3(0, _pinchOffset, 0));

            _sphereTarget.transform.position = transform.position + (direction * targetLength) - new Vector3(0, _pinchOffset, 0);
        }
    }
}