/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
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
        public GameObject buttonObject;
        public GameObject noseCone;
        [Tooltip("The local position which the button will be limited to and will try to return to.")]
        [SerializeField]
        private float buttonHeightLimit = 0.02f;
        [SerializeField]
        private float rocketPower = 30;
        [SerializeField]
        private float burnTime = 3;

        private const float BUTTON_PRESS_THRESHOLD = 0.01F;
        private const float BUTTON_PRESS_EXIT_THRESHOLD = 0.09F;

        private bool _isButtonPressed = false;

        private Rigidbody _rigidbody;

        bool launching = false;

        [SerializeField]
        ParticleSystem _particleSystem;

        void Start()
        {
            _rigidbody = this.GetComponent<Rigidbody>();
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

            _rigidbody.isKinematic = false;
            StartCoroutine(RocketBurn());
        }

        IEnumerator RocketBurn()
        {
            launching = true;

            _particleSystem.Play();

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

            launching = false;
            _particleSystem.Stop();
        }

        public void StopLaunch()
        {
            StopAllCoroutines();
            launching = false;
            _particleSystem.Stop();
        }
    }
}
