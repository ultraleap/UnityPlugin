/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

namespace Leap.Unity.PhysicalHands.Examples
{
    public class PhysicalHandsRocket : MonoBehaviour
    {
        [SerializeField, Tooltip("The pressable part of the rocket, which is used as a form of button")]
        private GameObject buttonObject;
        [SerializeField, Tooltip("Should be at the front of the rocket, used to calculate which direction the rocket should travel in.")]
        private GameObject noseCone;
        [Tooltip("The local position which the button will be limited to and will try to return to.")]
        [SerializeField]
        private float buttonHeightLimit = 0.02f;
        [SerializeField, Tooltip("Force to add to the rocket when launching.")]
        private float rocketPower = 30;
        [SerializeField, Tooltip("How long should the rocket add force for? (In seconds)")]
        private float burnTime = 3;

        private const float BUTTON_PRESS_THRESHOLD = 0.01F;
        private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09F;

        private bool _isButtonPressed = false;

        private Rigidbody _rigidbody;

        bool launching = false;

        [SerializeField, Tooltip("Particle system spawned from the rocket when launching. Usually smoke or engine particles.")]
        ParticleSystem _particleSystem;

        IgnorePhysicalHands _ignoreHands;

        void Start()
        {
            _rigidbody = this.GetComponent<Rigidbody>();
            _ignoreHands = this.GetComponent<IgnorePhysicalHands>();
        }

        void FixedUpdate()
        {
            if (buttonObject.transform.localPosition.y <= buttonHeightLimit * BUTTON_PRESS_THRESHOLD
                && !_isButtonPressed)
            {
                _isButtonPressed = true;
                ButtonPressed();
            }

            if (_isButtonPressed && buttonObject.transform.localPosition.y >= buttonHeightLimit * BUTTON_PRESS_EXIT_THRESHOLD)
            {
                _isButtonPressed = false;
            }
        }

        void ButtonPressed()
        {
            if (launching)
                return;

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                StartCoroutine(RocketBurn());

                if (_ignoreHands != null)
                {
                    _ignoreHands.DisableAllGrabbing = true;
                    _ignoreHands.DisableAllHandCollisions = true;
                }
            }
        }

        IEnumerator RocketBurn()
        {
            launching = true;
            if (_particleSystem != null)
            {
                _particleSystem.Play();
            }

            _rigidbody.angularDrag = 20;
            _rigidbody.useGravity = true;
            float timePassed = 0;
            while (timePassed < burnTime)
            {
                var heading = noseCone.transform.position - this.transform.position;
                _rigidbody.AddForceAtPosition(heading.normalized * rocketPower, transform.position, ForceMode.Acceleration);
                timePassed += Time.deltaTime;
                yield return null;
            }

            StopLaunch();
        }

        /// <summary>
        /// Stops rocket from adding force and stops the particle effect
        /// </summary>
        public void StopLaunch()
        {
            StopAllCoroutines();
            launching = false;

            if (_particleSystem != null)
            {
                _particleSystem.Stop();
            }

            if (_ignoreHands != null)
            {
                _ignoreHands.DisableAllGrabbing = false;
                _ignoreHands.DisableAllHandCollisions = false;
            }
        }
    }
}