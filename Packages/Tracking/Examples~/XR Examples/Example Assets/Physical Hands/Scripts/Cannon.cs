/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.Examples
{
    public class Cannon : MonoBehaviour
    {
        List<Rigidbody> innerObjects = new List<Rigidbody>();

        public float launchTimer = 2;

        public float explosionForce = 100;

        float timer = 0;

        private void Update()
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                timer = launchTimer;
                Launch();
            }
        }


        /// <summary>
        /// Add force to all objects which are within the cannon trigger volume
        /// </summary>
        public void Launch()
        {
            foreach (var obj in innerObjects)
            {
                obj.AddForce(transform.up * explosionForce);
            }

            innerObjects.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            var rbod = other.attachedRigidbody;
            var joint = other.GetComponent<Joint>();

            if (rbod != null && joint == null && !innerObjects.Contains(rbod))
            {
                innerObjects.Add(rbod);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var rbod = other.attachedRigidbody;

            if (rbod != null && innerObjects.Contains(rbod))
            {
                innerObjects.Remove(rbod);
            }
        }
    }
}