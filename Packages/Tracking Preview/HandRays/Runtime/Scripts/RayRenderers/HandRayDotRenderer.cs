using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public class HandRayDotRenderer : HandRayRenderer
    {
        public GameObject sphere;

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
                sphere.gameObject.SetActive(_isActive);
            }
            else
            {
                sphere.gameObject.SetActive(_isActive && _isRayEnabled);
            }
        }

        /// <summary>
        /// Sets whether the ray is over a valid point or not
        /// </summary>
        /// <param name="isValid"></param>
        public override void SetValid(bool isValid = true)
        {
            _isValid = isValid;
            sphere.GetComponent<MeshRenderer>().material.color = _isValid ? validColorGradient.colorKeys[0].color : invalidColorGradient.colorKeys[0].color;
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
            sphere.transform.position = handRayInteractor.linePoints[handRayInteractor.numPoints - 1];
        }
    }
}