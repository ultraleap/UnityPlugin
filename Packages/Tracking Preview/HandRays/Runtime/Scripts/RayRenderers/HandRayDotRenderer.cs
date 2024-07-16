/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Preview.HandRays
{
    public class HandRayDotRenderer : HandRayRenderer
    {
        // An object that represents the end of the ray
        public GameObject rayEndObject;

        /// <summary>
        /// Sets the ray renderer to be active or inactive
        /// When active, the ray renderer's visuals are enabled
        /// When inactive  the ray renderer's visuals are disabled
        /// </summary>
        /// <param name="isActive">Whether the ray renderer is active or not. Defaults to true</param>
        public override void SetActive(bool isActive = true)
        {
            _isActive = isActive;

            if (_ignoreRayInteractor)
            {
                rayEndObject.gameObject.SetActive(_isActive);
            }
            else
            {
                rayEndObject.gameObject.SetActive(_isActive && _isRayEnabled);
            }
        }

        protected override void OnRaycastStateChange(HandRayDirection direction, bool enabled)
        {
            _isRayEnabled = enabled;

            if (_ignoreRayInteractor)
            {
                rayEndObject.gameObject.SetActive(_isActive);
            }
            else
            {
                rayEndObject.gameObject.SetActive(_isActive && _isRayEnabled);
            }
        }


        /// <summary>
        /// Sets whether the ray is over a valid point or not
        /// </summary>
        /// <param name="isValid"></param>
        public override void SetValid(bool isValid = true)
        {
            _isValid = isValid;
            rayEndObject.GetComponent<MeshRenderer>().material.color = _isValid ? validColorGradient.colorKeys[0].color : invalidColorGradient.colorKeys[0].color;
        }

        protected override void OnRaycastUpdate(RaycastHit[] results, RaycastHit primaryHit)
        {
            if (_ignoreRayInteractor)
            {
                return;
            }

            if (!_isActive || !_isRayEnabled)
            {
                return;
            }

            if (hideRayOnNoPoints && (results == null || results.Length == 0))
            {
                lineRenderer.positionCount = 0;
                return;
            }

            lineRenderer.enabled = false;
            rayEndObject.transform.position = handRayInteractor.linePoints[handRayInteractor.numPoints - 1];
        }
    }
}