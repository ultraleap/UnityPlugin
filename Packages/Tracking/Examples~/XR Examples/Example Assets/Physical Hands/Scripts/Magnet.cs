/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Examples
{
    public class Magnet : MonoBehaviour
    {
        [Tooltip("How far away from each other can the magnets get before they stop being magnetic.")]
        public float distanceToDetach = 0.1f;

        [Tooltip("How many magnets can be attached to this one?")]
        public int maximumAttachedMagnets = 4;

        (ConfigurableJoint, Magnet)[] attachedMagnets;

        private void Awake()
        {
            attachedMagnets = new (ConfigurableJoint, Magnet)[maximumAttachedMagnets];
        }

        /// <summary>
        /// Determine whether an attachment to this magnet is available with another magnet
        /// </summary>
        public bool CanAttach(Magnet _toAttachTo)
        {
            int joinCount = 0;

            foreach (var join in attachedMagnets)
            {
                if (join.Item1 != null)
                {
                    if (join.Item2 == _toAttachTo)
                    {
                        return false;
                    }

                    joinCount++;
                }
            }

            if (joinCount >= maximumAttachedMagnets)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add the provided attachment to the array
        /// </summary>
        public void AddAttachment(ConfigurableJoint _joint, Magnet _magnet)
        {
            for (int i = 0; i < attachedMagnets.Length; i++)
            {
                if (attachedMagnets[i].Item1 == null)
                {
                    attachedMagnets[i].Item1 = _joint;
                    attachedMagnets[i].Item2 = _magnet;
                    return;
                }
            }
        }

        /// <summary>
        /// To allow users to detach objects, we use distance-based checks.
        /// This is to avoid force issues when moving kinematic objects.
        /// </summary>
        private void FixedUpdate()
        {
            for (int i = 0; i < attachedMagnets.Length; i++)
            {
                if (attachedMagnets[i].Item1 != null)
                {
                    if (Vector3.Distance(transform.position, attachedMagnets[i].Item2.transform.position) > distanceToDetach)
                    {
                        Destroy(attachedMagnets[i].Item1);
                    }
                }
            }
        }

        /// <summary>
        /// Remove all attached objects, great for resetting objects and turning of magnet all together
        /// </summary>
        public void RemoveAllAttachments()
        {
            for (int i = 0; i < attachedMagnets.Length; i++)
            {
                if (attachedMagnets[i].Item1 != null)
                {
                    Destroy(attachedMagnets[i].Item1);
                    attachedMagnets[i].Item1 = null;
                    attachedMagnets[i].Item2.RemoveAllAttachments();
                }
            }
        }
    }
}