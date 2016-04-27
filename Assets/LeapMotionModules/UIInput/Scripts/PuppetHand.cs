/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2016.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity
{
    /** A physics model for our rigid hand made out of various Unity Collider. */
    public class PuppetHand : SkeletalHand
    {
        public override ModelType HandModelType
        {
            get
            {
                return ModelType.Physics;
            }
        }
        public float filtering = 0.5f;

        public override void InitHand()
        {
            base.InitHand();

            for (int f = 0; f < fingers.Length; ++f)
            {
                if (fingers[f] != null)
                {
                     Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), palm.GetComponent<Collider>());
                     Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), palm.GetComponent<Collider>());

                     if (f < fingers.Length - 1)
                     {
                         Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), fingers[f + 1].bones[1].GetComponent<Collider>());
                         Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), fingers[f + 1].bones[2].GetComponent<Collider>());
                         Physics.IgnoreCollision(fingers[f].bones[3].GetComponent<Collider>(), fingers[f + 1].bones[3].GetComponent<Collider>());
                         Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), fingers[f + 1].bones[3].GetComponent<Collider>());
                         Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), fingers[f + 1].bones[3].GetComponent<Collider>());
                     }
                }
            }
        }

        public void OnEnable()
        {
            for (int f = 0; f < fingers.Length; ++f)
            {
                if (fingers[f] != null)
                {
                    Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), palm.GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), palm.GetComponent<Collider>());

                    if (f < fingers.Length - 1)
                    {
                        Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), fingers[f + 1].bones[1].GetComponent<Collider>());
                        Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), fingers[f + 1].bones[2].GetComponent<Collider>());
                        Physics.IgnoreCollision(fingers[f].bones[3].GetComponent<Collider>(), fingers[f + 1].bones[3].GetComponent<Collider>());
                        Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), fingers[f + 1].bones[3].GetComponent<Collider>());
                        Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), fingers[f + 1].bones[3].GetComponent<Collider>());
                    }
                }
            }
            Rigidbody palmBody = palm.GetComponent<Rigidbody>();
            palmBody.velocity = Vector3.zero;
            palmBody.angularVelocity = Vector3.zero;
        }

        public override void UpdateHand()
        {
            if (palm != null)
            {
                Rigidbody palmBody = palm.GetComponent<Rigidbody>();
                if (palmBody)
                {
                    palmBody.MovePosition(GetPalmCenter());
                    palmBody.MoveRotation(GetPalmRotation());
                    palmBody.velocity *= 0.95f;
                    palmBody.angularVelocity *= 0.95f;
                    palm.position = GetPalmCenter();
                    palm.rotation = GetPalmRotation();
                }
                else
                {
                    palm.position = GetPalmCenter();
                    palm.rotation = GetPalmRotation();
                }
            }


            for (int f = 0; f < fingers.Length; ++f)
            {
                if (fingers[f] != null)
                {
                    fingers[f].UpdateFinger();
                }
            }

            if (forearm != null)
            {
                // Set arm dimensions.
                CapsuleCollider capsule = forearm.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    // Initialization
                    capsule.direction = 2;
                    forearm.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

                    // Update
                    capsule.radius = GetArmWidth() / 2f;
                    capsule.height = GetArmLength() + GetArmWidth();

                    Physics.IgnoreCollision(capsule, palm.GetComponent<Collider>());
                }

                Rigidbody forearmBody = forearm.GetComponent<Rigidbody>();
                if (forearmBody)
                {
                    forearmBody.MovePosition(GetArmCenter());
                    forearmBody.MoveRotation(GetArmRotation());
                }
                else
                {
                    forearm.position = GetArmCenter();
                    forearm.rotation = GetArmRotation();
                }
            }
        }
    }
}
