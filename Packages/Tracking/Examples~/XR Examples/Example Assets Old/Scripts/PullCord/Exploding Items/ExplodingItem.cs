/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

namespace Leap.Unity.Examples
{
    /// <summary>
    /// An exploding item has a start position and an end position and can linearly move between the two given a percent value.
    /// Use SetPercent() to change this value.
    /// </summary>
    public class ExplodingItem : MonoBehaviour
    {
        [SerializeField, Tooltip("Local positions")]
        private Vector3 _startPosition, _endPosition;

        private float _currentPercent = 0f, _desiredPercent = 0f;

        [SerializeField]
        private float _lerpSpeed = 0.05f;

        private bool _isLerping = false;

        private void Awake()
        {
            transform.localPosition = _startPosition;
        }

        /// <summary>
        /// Set percent of the explosion. This sets the local position to a position between the _startPosition and _endPosition by percent.
        /// </summary>
        /// <param name="percent">desired percent</param>
        /// <param name="instant">If true, the position is instantly updated without a lerp. False by default</param>
        public void SetPercent(float percent, bool instant = false)
        {
            _desiredPercent = percent;

            if (instant || Mathf.Abs(_currentPercent - _desiredPercent) < 1e-8)
            {
                _currentPercent = _desiredPercent;
                UpdateLerpPosition();
            }
            else
            {
                if (!_isLerping)
                {
                    StartCoroutine(LerpRoutine());
                }
            }
        }

        private void UpdateLerpPosition()
        {
            transform.localPosition = Vector3.Lerp(_startPosition, _endPosition, _currentPercent);
        }

        private IEnumerator LerpRoutine()
        {
            _isLerping = true;
            while (Mathf.Abs(_currentPercent - _desiredPercent) > 1e-8)
            {
                _currentPercent = Mathf.Lerp(_currentPercent, _desiredPercent, Time.deltaTime * (1.0f / _lerpSpeed));
                UpdateLerpPosition();
                yield return new WaitForEndOfFrame();
            }
            _currentPercent = _desiredPercent;
            UpdateLerpPosition();
            _isLerping = false;
        }
    }
}