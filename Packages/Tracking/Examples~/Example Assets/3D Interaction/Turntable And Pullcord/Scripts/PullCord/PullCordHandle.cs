/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using Leap.Unity.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples
{
    /// <summary>
    /// PullCordHandle keeps track of the handle's position and moves it according to hand attraction and pinching.
    /// </summary>
    [RequireComponent(typeof(InteractionBehaviour))]
    public class PullCordHandle : MonoBehaviour
    {
        [SerializeField, Tooltip("Max hover distance of hand where the handle pos is still attracted to it")]
        private float _distanceToHandThreshold = 0.09f;
        [SerializeField, Tooltip("Distance that the handle can be pulled away from its resting position")]
        private float _stretchThreshold = 0.6f;

        [SerializeField, Tooltip("These gameobjects will be disabled, while the handle is not pinched")]
        private List<GameObject> _objectsToDisable;
        [SerializeField, Tooltip("Sphere that is attached to the handle")]
        private Transform _handleSphere;
        [SerializeField]
        private Transform _hint;

        private Vector3 _restingPos;
        /// <summary>
        /// The position that the handle is going to jump to if released
        /// </summary>
        [HideInInspector] public Vector3 RestingPos { get { return _restingPos; } set { _restingPos = value; } }

        private InteractionBehaviour _intBeh;
        private Vector3 _handlePositionTarget;

        private float _springSpeed = 100f;
        private Vector3 _vposition;

        private float _pinchActivateDistance = 0.03f;
        private float _pinchDeactivateDistance = 0.04f;
        private bool isPinching = false;


        private void OnEnable()
        {
            _intBeh = GetComponent<InteractionBehaviour>();
            StopPinching();
        }

        private void Update()
        {
            _handlePositionTarget = RestingPos;

            if (_intBeh.isPrimaryHovered)
            {
                Vector3 midpoint = Midpoint(_intBeh.primaryHoveringHand);
                float distance = Vector3.Distance(midpoint, RestingPos);
                UpdatePinching(_intBeh.primaryHoveringHand, distance);

                if (isPinching || distance < _distanceToHandThreshold)
                {
                    _handlePositionTarget = midpoint;
                }
            }
            else if (isPinching)
            {
                StopPinching();
            }

            transform.position = SpringPosition(transform.position, _handlePositionTarget);
        }

        private Vector3 SpringPosition(Vector3 current, Vector3 target)
        {
            _vposition = Step(_vposition, Vector3.zero, _springSpeed * 0.35f);
            _vposition += (target - current) * (_springSpeed * 0.1f);
            return current + _vposition * Time.deltaTime;
        }

        private Vector3 Step(Vector3 current, Vector3 target, float omega)
        {
            var exp = Mathf.Exp(-omega * Time.deltaTime);
            return Vector3.Lerp(target, current, exp);
        }

        private Vector3 Midpoint(Leap.Hand hand)
        {
            Vector3 indexPos = hand.GetIndex().TipPosition;
            Vector3 thumbPos = hand.GetThumb().TipPosition;
            return (indexPos + thumbPos) / 2;
        }

        private void UpdatePinching(Leap.Hand hand, float distanceToRestingPos)
        {
            Vector3 indexPos = hand.GetIndex().TipPosition;
            Vector3 thumbPos = hand.GetThumb().TipPosition;
            float distance = Vector3.Distance(indexPos, thumbPos);

            if (isPinching && (distance > _pinchDeactivateDistance || distanceToRestingPos > _stretchThreshold))
            {
                StopPinching();
            }

            // only start pinching if the midpoint is close enough to the resting position
            else if (distanceToRestingPos < _distanceToHandThreshold)
            {
                if (!isPinching && distance < _pinchActivateDistance)
                {
                    StartPinching();
                }

                // update handle sphere size
                _handleSphere.localScale = Vector3.one * Mathf.Min(distance * 20f, 1f);

                // update hint size if it is active (if this is the first pinch)
                if (isPinching && _hint.gameObject.activeSelf)
                {
                    _hint.localScale *= 0.9f;

                    // hide hint if its scale is very small
                    if (_hint.localScale.x < 0.01f)
                    {
                        _hint.gameObject.SetActive(false);
                    }
                }
            }

        }

        private void StartPinching()
        {
            isPinching = true;

            // show objects that should be disabled
            foreach (GameObject go in _objectsToDisable)
            {
                go.SetActive(true);
            }
        }

        private void StopPinching()
        {
            // hide hint if it has been pinched
            if (isPinching && _hint.gameObject.activeSelf)
            {
                _hint.gameObject.SetActive(false);
            }

            isPinching = false;

            // hide objects that should be disabled
            foreach (GameObject go in _objectsToDisable)
            {
                go.SetActive(false);
            }

            // reset handle sphere size
            _handleSphere.localScale = Vector3.one;
        }

    }
}