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
    /// <summary>
    /// PullCord updates the handle projection position, 
    /// updates the resting position for the pullCordHandle,
    /// provides a value for the progress of the pull cord
    /// </summary>
    public class PullCord : MonoBehaviour
    {
        [SerializeField] private Transform _pullCordStart, _pullCordEnd;
        [SerializeField] private PullCordHandle _pullCordHandle;

        [SerializeField, Tooltip("This is the projection of the handle onto the line between start and end positions")]
        private Transform _handleProjection;

        [SerializeField, Tooltip("Between 0 and 1. The pullCordHandle will be at this pos by default when it is collapsed")]
        private float _restingPoint = 0.2f;
        [SerializeField, Tooltip("Between 0 and 1. If pullCord progress exceeds this number, the handle's resting position is changed to the pullCord end")]
        private float _pulledPoint = 0.7f;
        [SerializeField, Tooltip("If the progress changes by at least this much towards the pull cord end in one frame, the pull cord completes the pull")]
        private float _progressVelocityThreshold = 0.005f;

        /// <summary>
        /// Dispatched when the Progress of the pull cord gets bigger than the _pulledPoint
        /// </summary>
        public UnityEvent OnPulled;

        /// <summary>
        /// Dispatched when the Progress of the pull cord gets smaller than the _pulledPoint
        /// </summary>
        public UnityEvent OnUnpulled;

        /// <summary>
        /// Dispatched when the progress of the pull cord changes. Progress is between 0 and 1
        /// </summary>
        public UnityEvent<float> OnProgressChanged;

        private float _progress;
        /// <summary>
        /// Progress of the pull cord. 0 means it is fully collapsed, 1 means it is fully pulled.
        /// </summary>
        public float Progress => _progress;

        private Vector3 _defaultRestingPos;
        private bool _pulled = false;

        private float _pullCordLength;

        private float _currentProgressVelocity;

        private void Start()
        {
            _pullCordLength = Vector3.Distance(_pullCordStart.position, _pullCordEnd.position);

            _defaultRestingPos = _pullCordStart.position + _restingPoint * (_pullCordEnd.position - _pullCordStart.position);
            _pullCordHandle.RestingPos = _defaultRestingPos;

            _pullCordHandle.OnPinchEnd.AddListener(OnPinchEnd);
        }

        private void OnDestroy()
        {
            _pullCordHandle.OnPinchEnd.RemoveListener(OnPinchEnd);
        }


        private void Update()
        {
            UpdateProjection();

            float newProgress = Vector3.Distance(_pullCordStart.position, _handleProjection.position) / _pullCordLength;
            _currentProgressVelocity = newProgress - Progress;

            if (newProgress == Progress)
            {
                return;
            }

            _progress = newProgress;

            // Ignore the first bit of the progress as that is where the handle's resting pos is
            float restinglessProgress = (Progress - _restingPoint) / (1 - _restingPoint);
            OnProgressChanged?.Invoke(restinglessProgress);

            if (_pullCordHandle.State == PullCordHandle.PullCordState.Pinched)
            {
                // update resting pos of handle, if the pulled status has changed since last frame:
                if (!_pulled && _progress > _pulledPoint)
                {
                    if (OnPulled != null) OnPulled.Invoke();
                    _pulled = true;
                    _pullCordHandle.RestingPos = _pullCordEnd.position;
                }
                else if (_pulled && _progress < _pulledPoint)
                {
                    if (OnUnpulled != null) OnUnpulled.Invoke();
                    _pulled = false;
                    _pullCordHandle.RestingPos = _defaultRestingPos;
                }
            }
        }

        private void UpdateProjection()
        {
            // get projection pos -> the closest point to the handle pos that is on the line between start and end positions
            Vector3 direction = _pullCordEnd.position - _pullCordStart.position;
            float length = direction.magnitude;
            direction.Normalize();
            float projectionLength = Mathf.Clamp(Vector3.Dot(_pullCordHandle.transform.position - _pullCordStart.position, direction), 0f, length);
            _handleProjection.position = _pullCordStart.position + direction * projectionLength;
        }

        private void OnPinchEnd()
        {
            // if the velocity of the progress is bigger than some threshold, set the resting position to the pulled position
            if (_currentProgressVelocity > _progressVelocityThreshold)
            {
                if (OnPulled != null) OnPulled.Invoke();
                _pulled = true;
                _pullCordHandle.RestingPos = _pullCordEnd.position;
            }
        }
    }
}