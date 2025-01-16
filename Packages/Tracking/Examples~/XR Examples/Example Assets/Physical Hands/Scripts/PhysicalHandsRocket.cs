/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

namespace Leap.PhysicalHands.Examples
{
    public class PhysicalHandsRocket : MonoBehaviour
    {
        [SerializeField, Tooltip("Should be at the front of the rocket, used to calculate which direction the rocket should travel in.")]
        private GameObject noseCone;
        [SerializeField, Tooltip("Force to add to the rocket when launching.")]
        private float rocketPower = 30;
        [SerializeField, Tooltip("How long should the rocket add force for? (In seconds)")]
        private float burnTime = 3;

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


        public void ButtonPressed()
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
                    _ignoreHands.DisableCollisionOnChildObjects = true;
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

#if UNITY_6000_0_OR_NEWER
            _rigidbody.angularDamping = 20;
#else
            _rigidbody.angularDrag = 20;
#endif
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
                _ignoreHands.DisableCollisionOnChildObjects = false;
            }
        }
    }
}
