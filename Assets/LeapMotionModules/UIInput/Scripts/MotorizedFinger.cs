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
    /** A physics finger model for our rigid hand made out of various cube Unity Colliders. */
    public class MotorizedFinger : SkeletalFinger
    {

        public float filtering = 0.5f;
        public bool update = true;
        public Rigidbody Palm;

        void Start()
        {
            for (int i = 0; i < bones.Length; ++i)
            {
                if (bones[i] != null)
                {
                    bones[i].GetComponent<Rigidbody>().maxAngularVelocity = 7f;
                    bones[i].GetComponent<Rigidbody>().maxDepenetrationVelocity = 1f;

                    //transform.parent = null;
                    //Palm.transform.parent = null;
                    //bones[i].parent = null;

                    if (i == 1)
                    {
                        HingeJoint Hinge = Palm.gameObject.AddComponent<HingeJoint>();
                        Hinge.enableCollision = false;
                        Hinge.enablePreprocessing = true;
                        Hinge.autoConfigureConnectedAnchor = false;
                        Hinge.connectedBody = bones[i].GetComponent<Rigidbody>();
                        Hinge.anchor = Palm.transform.InverseTransformPoint(bones[i].TransformPoint(new Vector3(0f, 0f, -bones[i].GetComponent<CapsuleCollider>().height / 2f)));
                        Hinge.connectedAnchor = new Vector3(0f, 0f, -bones[i].GetComponent<CapsuleCollider>().height / 2f);
                        Hinge.axis = Palm.transform.InverseTransformDirection(bones[i].transform.right);

                        Hinge.useMotor = true;
                        JointMotor mmotor = new JointMotor();
                        mmotor.force = 1000f;
                        mmotor.targetVelocity = -100f;
                        Hinge.motor = mmotor;


                    }

                    if (i + 1 < bones.Length)
                    {
                        HingeJoint Hinge = bones[i].gameObject.AddComponent<HingeJoint>();
                        Hinge.enableCollision = false;
                        Hinge.enablePreprocessing = true;
                        Hinge.autoConfigureConnectedAnchor = true;
                        Hinge.connectedBody = bones[i + 1].gameObject.GetComponent<Rigidbody>();
                        Hinge.anchor = bones[i].InverseTransformPoint(bones[i + 1].TransformPoint(new Vector3(0f, 0f, -bones[i + 1].GetComponent<CapsuleCollider>().height / 2f)));
                        //Hinge.connectedAnchor = new Vector3(0f, 0f, -bones[i+1].GetComponent<CapsuleCollider>().height / 2f);
                        Hinge.axis = bones[i].InverseTransformDirection(bones[i + 1].transform.right);

                        Hinge.useMotor = true;
                        JointMotor mmotor = new JointMotor();
                        mmotor.force = 1000f;
                        mmotor.targetVelocity = -100f;
                        Hinge.motor = mmotor;
                    }

                }
            }
        }
        public void Update()
        {
            for (int i = 0; i < bones.Length; ++i)
            {
                if (bones[i] != null)
                {
                    Rigidbody boneBody = bones[i].GetComponent<Rigidbody>();
                    boneBody.velocity = Vector3.zero;
                    boneBody.angularVelocity = Vector3.zero;
                }
            }
        }


        public override void UpdateFinger()
        {

            for (int i = 0; i < bones.Length; ++i)
            {
                if (bones[i] != null)
                {
                    // Set bone dimensions.
                    CapsuleCollider capsule = bones[i].GetComponent<CapsuleCollider>();
                    if (capsule != null)
                    {
                        // Initialization
                        //capsule.direction = 2;
                        // bones[i].localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

                        // Update
                        //capsule.radius = GetBoneWidth(i) / 2f;
                        //capsule.height = GetBoneLength(i) + GetBoneWidth(i);
                    }

                    Rigidbody boneBody = bones[i].GetComponent<Rigidbody>();
                    if (update)
                    {
                        if (boneBody)
                        {
                            boneBody.MovePosition(GetBoneCenter(i));
                            boneBody.MoveRotation(GetBoneRotation(i));
                        }
                        else
                        {
                            bones[i].position = GetBoneCenter(i);
                            bones[i].rotation = GetBoneRotation(i);
                        }
                    }
                }
            }
        }
    }
}
