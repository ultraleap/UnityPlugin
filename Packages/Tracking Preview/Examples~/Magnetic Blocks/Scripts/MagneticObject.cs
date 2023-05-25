/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Leap.Unity.Interaction
{
    public class MagneticObject : MonoBehaviour
    {
        const float NEARBY_MAGNETICS_RADIUS = 0.02f;

        Magnet[] magneticPoints;
        public Rigidbody rbody;

        [Space, Header("Magnet Values")]
        [Tooltip("The furthest two magnets can be before they begin attracting")]
        public float maximumMagneticPullDistance = 0.02f;
        [Tooltip("The nearest two magnets can be before they are joined")]
        public float minimumMagneticPullDistance = 0.002f;

        public float magneticPullStrength = 5;

        List<MagneticObject> nearbyMagneticObjects = new List<MagneticObject>();

        private void Awake()
        {
            if (rbody == null)
            {
                rbody = GetComponent<Rigidbody>();
            }

            magneticPoints = GetComponentsInChildren<Magnet>();
        }

        private void FixedUpdate()
        {
            FindNearbyMagneticObjects();
            ApplyMagnets();
        }

        /// <summary>
        /// Find all nearby MagneticObjects by checking a radius around each of our Magnets.
        /// Store the result in a list for later use
        /// </summary>
        void FindNearbyMagneticObjects()
        {
            nearbyMagneticObjects.Clear();

            foreach (var magnet in magneticPoints)
            {
                var nearbyColliders = Physics.OverlapSphere(magnet.transform.position, NEARBY_MAGNETICS_RADIUS);

                foreach (var collider in nearbyColliders)
                {
                    MagneticObject otherMagnet = collider.GetComponentInParent<MagneticObject>();

                    if (otherMagnet != null && otherMagnet != this && !nearbyMagneticObjects.Contains(otherMagnet))
                    {
                        nearbyMagneticObjects.Add(otherMagnet);
                    }
                }
            }
        }

        /// <summary>
        /// Magnets within maximumMagneticPullDistance of eachother will attract, once within mainimumMagneticPullDistance
        /// the Magnets will become attached by a ConfigurableJoint
        /// </summary>
        void ApplyMagnets()
        {
            foreach (var magneticPoint in magneticPoints)
            {
                if (!magneticPoint.CanAttach(magneticPoint))
                {
                    continue;
                }

                foreach (var otherMagnet in nearbyMagneticObjects)
                {
                    var nearestOtherMagneticPoint = otherMagnet.magneticPoints.
                        Where(point => point.CanAttach(magneticPoint)).
                        OrderBy(point => Vector3.Distance(point.transform.position, magneticPoint.transform.position)).
                        FirstOrDefault();

                    if (nearestOtherMagneticPoint == null)
                    {
                        continue;
                    }

                    Vector3 direction = nearestOtherMagneticPoint.transform.position - magneticPoint.transform.position;

                    if (direction.magnitude < minimumMagneticPullDistance)
                    {
                        var joint = gameObject.AddComponent<ConfigurableJoint>();
                        joint.connectedBody = otherMagnet.rbody;
                        joint.anchor = magneticPoint.transform.localPosition;
                        joint.xMotion = ConfigurableJointMotion.Locked;
                        joint.yMotion = ConfigurableJointMotion.Locked;
                        joint.zMotion = ConfigurableJointMotion.Locked;
                        joint.projectionMode = JointProjectionMode.PositionAndRotation;
                        joint.projectionDistance = 0.01f;
                        joint.enableCollision = true;

                        magneticPoint.AddAttachment(joint, nearestOtherMagneticPoint);
                        nearestOtherMagneticPoint.AddAttachment(joint, magneticPoint);
                    }
                    else if (direction.magnitude < maximumMagneticPullDistance)
                    {
                        rbody.AddForceAtPosition(direction.normalized * magneticPullStrength, magneticPoint.transform.position);
                    }
                }
            }
        }
    }
}