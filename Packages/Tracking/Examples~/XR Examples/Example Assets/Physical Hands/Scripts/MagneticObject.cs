/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Examples
{
    public class MagneticObject : MonoBehaviour
    {
        Magnet[] magneticPoints;
        public Rigidbody rbody;

        [Space, Header("Magnet Values")]
        [Tooltip("The furthest two magnets can be before they begin attracting")]
        public float maximumMagneticPullDistance = 0.02f;
        [Tooltip("The nearest two magnets can be before they are joined")]
        public float minimumMagneticPullDistance = 0.002f;

        [SerializeField]
        private float magneticPullStrength = 5;
        [SerializeField, Tooltip("How much force can the magnetic joint take before it breaks?")]
        private float jointBreakForce = 100;


        private float magneticAngularLimitDampening = 1;

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
                var nearbyColliders = Physics.OverlapSphere(magnet.transform.position, maximumMagneticPullDistance);

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
                        SoftJointLimitSpring angularLimits = new SoftJointLimitSpring();

                        angularLimits.damper = magneticAngularLimitDampening;

                        var joint = gameObject.AddComponent<ConfigurableJoint>();
                        joint.connectedBody = otherMagnet.rbody;
                        joint.anchor = magneticPoint.transform.localPosition;
                        joint.xMotion = ConfigurableJointMotion.Locked;
                        joint.yMotion = ConfigurableJointMotion.Locked;
                        joint.zMotion = ConfigurableJointMotion.Locked;

                        joint.angularXLimitSpring = angularLimits;
                        joint.angularYZLimitSpring = angularLimits;

                        joint.rotationDriveMode = RotationDriveMode.Slerp;
                        joint.projectionMode = JointProjectionMode.PositionAndRotation;
                        joint.projectionDistance = 0.01f;
                        joint.enableCollision = true;

                        joint.breakForce = jointBreakForce;

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

        /// <summary>
        /// Force this magnetic object to detach from all attached magnets
        /// </summary>
        public void ForceDetatchMagnets()
        {
            foreach (var magnet in magneticPoints)
            {
                magnet.RemoveAllAttachments();
            }

            nearbyMagneticObjects.Clear();
        }
    }
}