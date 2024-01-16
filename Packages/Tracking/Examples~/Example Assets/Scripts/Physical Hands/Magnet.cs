/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Interaction
{
    public class Magnet : MonoBehaviour
    {
        public float DISTANCE_TO_DETACH = 0.1f;

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
                    if (Vector3.Distance(transform.position, attachedMagnets[i].Item2.transform.position) > DISTANCE_TO_DETACH)
                    {
                        Destroy(attachedMagnets[i].Item1);
                    }
                }
            }
        }

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