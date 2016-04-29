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
        public float PalmSpringStrength = 10000f;
        public float PalmSpringDistanceOfMaxForce = 0.03f;
        public float FingerMotorForce = 1000f;
        public float FingerMotorSpeed = 50f;
        public float FingerMass = 10f;
        public bool DetachHand = false;
        public bool DisableSprings = true;
        public bool DisableMotors = true;


        //Holder Parent of all of the Rigidbodies this hand is made of
        GameObject RigidbodyParent;

        //Springs that hold the Palm to the Tracked Position
        SpringJoint spring1;
        SpringJoint spring2;
        SpringJoint spring3;
        SpringJoint spring4;

        public override ModelType HandModelType
        {
            get
            {
                return ModelType.Physics;
            }
        }

        public override void InitHand()
        {
            base.InitHand();

            if (Application.isPlaying) {
                RigidbodyParent = new GameObject();
                RigidbodyParent.transform.parent = null;
                RigidbodyParent.transform.position = Vector3.zero;
                RigidbodyParent.name = gameObject.name + "'s Physics Parent";
                palm.parent = RigidbodyParent.transform;


                for (int f = 0; f < fingers.Length; ++f) {
                    if (fingers[f] != null) {
                        //General Finger Options
                        ((MotorizedFinger)fingers[f]).setParentofDigits(RigidbodyParent.transform, FingerMotorForce, FingerMotorSpeed, GetComponent<Rigidbody>().useGravity, !DisableMotors, FingerMass);

                        DisableFingerCollisions();
                    }
                }
            }
        }

        void DisableFingerCollisions()
        {
            for (int f = 0; f < fingers.Length; ++f) {
                if (fingers[f] != null) {
                    Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), forearm.GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), palm.GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), palm.GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[3].GetComponent<Collider>(), palm.GetComponent<Collider>());

                    Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), fingers[f].bones[2].GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[1].GetComponent<Collider>(), fingers[f].bones[3].GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), fingers[f].bones[1].GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[2].GetComponent<Collider>(), fingers[f].bones[3].GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[3].GetComponent<Collider>(), fingers[f].bones[1].GetComponent<Collider>());
                    Physics.IgnoreCollision(fingers[f].bones[3].GetComponent<Collider>(), fingers[f].bones[2].GetComponent<Collider>());

                    if (f < fingers.Length - 1) {
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
            if (Application.isPlaying) {
                if (palm != null) {
                    palm.gameObject.SetActive(true);
                }

                DisableFingerCollisions();

                Rigidbody palmBody = palm.GetComponent<Rigidbody>();
                palmBody.velocity = Vector3.zero;
                palmBody.angularVelocity = Vector3.zero;
                palmBody.useGravity = GetComponent<Rigidbody>().useGravity;
            }
        }

        void OnDisable()
        {
            if (Application.isPlaying) {
                if (palm != null) {
                    palm.gameObject.SetActive(false);
                }

                SpringJoint[] springlist = gameObject.GetComponents<SpringJoint>();
                foreach (SpringJoint foundspring in springlist) {
                    Destroy(foundspring);
                }
            }
        }

        public override void UpdateHand()
        {
            if (Application.isPlaying)
            {
                if (palm != null)
                {
                    Rigidbody palmBody = palm.GetComponent<Rigidbody>();
                    if (palmBody)
                    {
                        if (!palmBody.isKinematic) {
                            if (!gameObject.GetComponent<SpringJoint>()) {
                                if (!DetachHand) {
                                    transform.position = GetPalmCenter();
                                    transform.rotation = GetPalmRotation();

                                    if (!DisableSprings) {
                                        spring1 = gameObject.AddComponent<SpringJoint>();
                                        spring1.anchor = new Vector3(0.03f, 0f, 0.03f);
                                        spring1.autoConfigureConnectedAnchor = false;
                                        spring1.damper = 0f;
                                        spring1.maxDistance = 0f;
                                        spring1.tolerance = 0f;
                                        spring1.connectedBody = palmBody;
                                        spring1.connectedAnchor = new Vector3(0.03f, 0f, 0.03f);
                                        spring1.transform.position = GetPalmCenter();

                                        spring2 = gameObject.AddComponent<SpringJoint>();
                                        spring2.anchor = new Vector3(-0.03f, 0f, 0.03f);
                                        spring2.autoConfigureConnectedAnchor = false;
                                        spring2.damper = 0f;
                                        spring2.maxDistance = 0f;
                                        spring2.tolerance = 0f;
                                        spring2.connectedBody = palmBody;
                                        spring2.connectedAnchor = new Vector3(-0.03f, 0f, 0.03f);
                                        spring2.transform.position = GetPalmCenter();

                                        spring3 = gameObject.AddComponent<SpringJoint>();
                                        spring3.anchor = new Vector3(-0.03f, 0f, -0.03f);
                                        spring3.autoConfigureConnectedAnchor = false;
                                        spring3.damper = 0f;
                                        spring3.maxDistance = 0f;
                                        spring3.tolerance = 0f;
                                        spring3.connectedBody = palmBody;
                                        spring3.connectedAnchor = new Vector3(-0.03f, 0f, -0.03f);
                                        spring3.transform.position = GetPalmCenter();

                                        spring4 = gameObject.AddComponent<SpringJoint>();
                                        spring4.anchor = new Vector3(0.03f, 0f, -0.03f);
                                        spring4.autoConfigureConnectedAnchor = false;
                                        spring4.damper = 0f;
                                        spring4.maxDistance = 0f;
                                        spring4.tolerance = 0f;
                                        spring4.connectedBody = palmBody;
                                        spring4.connectedAnchor = new Vector3(0.03f, 0f, -0.03f);
                                        spring4.transform.position = GetPalmCenter();
                                    } else {

                                        palmBody.velocity = ((transform.position - palmBody.position) / Time.fixedDeltaTime);

                                        Quaternion palmRot = transform.rotation;
                                        float dot = Quaternion.Dot(palmBody.rotation, palmRot);
                                        if (dot > 0f) { palmRot = new Quaternion(-palmRot.x, -palmRot.y, -palmRot.z, -palmRot.w); }
                                        Vector3 axis; float angle;
                                        Quaternion localQuat = palmBody.rotation * Quaternion.Inverse(palmRot);
                                        localQuat.ToAngleAxis(out angle, out axis);
                                        axis *= angle;
                                        if ((axis / Time.fixedDeltaTime).x != Mathf.Infinity && (axis / Time.fixedDeltaTime).x != Mathf.NegativeInfinity) {
                                            palmBody.angularVelocity = (axis / Time.fixedDeltaTime);
                                        }
                                    }
                                }
                            } else {
                                transform.position = GetPalmCenter();
                                transform.rotation = GetPalmRotation();
                                GetComponent<Rigidbody>().MovePosition(GetPalmCenter());
                                GetComponent<Rigidbody>().MoveRotation(GetPalmRotation());

                                if (!DisableSprings) {
                                    float dist = Vector3.Distance(transform.TransformPoint(spring1.anchor), palm.TransformPoint(spring1.connectedAnchor));
                                    spring1.spring = (dist > PalmSpringDistanceOfMaxForce) ? (PalmSpringStrength * PalmSpringDistanceOfMaxForce) / dist : PalmSpringStrength;
                                    dist = Vector3.Distance(transform.TransformPoint(spring2.anchor), palm.TransformPoint(spring2.connectedAnchor));
                                    spring2.spring = (dist > PalmSpringDistanceOfMaxForce) ? (PalmSpringStrength * PalmSpringDistanceOfMaxForce) / dist : PalmSpringStrength;
                                    dist = Vector3.Distance(transform.TransformPoint(spring3.anchor), palm.TransformPoint(spring3.connectedAnchor));
                                    spring3.spring = (dist > PalmSpringDistanceOfMaxForce) ? (PalmSpringStrength * PalmSpringDistanceOfMaxForce) / dist : PalmSpringStrength;
                                    dist = Vector3.Distance(transform.TransformPoint(spring4.anchor), palm.TransformPoint(spring4.connectedAnchor));
                                    spring4.spring = (dist > PalmSpringDistanceOfMaxForce) ? (PalmSpringStrength * PalmSpringDistanceOfMaxForce) / dist : PalmSpringStrength;
                                }
                            }
                        } else {
                            palmBody.MovePosition(GetPalmCenter());
                            palmBody.MoveRotation(GetPalmRotation());
                        }
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
                        //Setting the Tracked Palm Transform, so Fingers can drive relative to their physical palm using Transforms that have been put into local-to-tracked-palm space
                        ((MotorizedFinger)fingers[f]).setPalmTransform(GetPalmCenter(), GetPalmRotation());
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
}
