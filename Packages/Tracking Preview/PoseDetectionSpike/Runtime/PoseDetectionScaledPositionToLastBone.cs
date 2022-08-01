//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Leap;
//using Leap.Unity;

//public class PoseDetectionScaledPositionToLastBone : PoseDetectionBase
//{
//    public Transform testBone;
//    public float maxDistance = 0.05f;

//    Color[] capsuleHandColours;

//    List<Vector3> savedBonePositions;

//    // Start is called before the first frame update
//    void Start()
//    {
//        capsuleHand.SetIndividualSphereColors = true;
//        capsuleHandColours = capsuleHand.SphereColors;

//        boneErrors = new float[20];
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.Space))
//        {
//            savedBonePositions = new List<Vector3>();
//            Hand leapHand = capsuleHand.GetLeapHand();

//            // loop through bones
//            for (int i = 0; i < 5; i++)
//            {
//                for (int j = 0; j < 4; j++)
//                {
//                    Vector3 bonePosition = leapHand.Fingers[i].bones[j].NextJoint.ToVector3();

//                    var prevJointPosition = leapHand.Fingers[i].bones[j].PrevJoint.ToVector3();

//                    Vector3 localBonePosition = Quaternion.Inverse(leapHand.Fingers[i].bones[j].Rotation.ToQuaternion()) * (bonePosition - prevJointPosition);

//                    savedBonePositions.Add(localBonePosition);
//                }
//            }
//        }
//        else if (savedBonePositions != null)
//        {
//            Hand leapHand = capsuleHand.GetLeapHand();

//            if (leapHand == null) return;

//            for (int i = 0; i < 5; i++)
//            {
//                for (int j = 0; j < 4; j++)
//                {
//                    Vector3 bonePosition = leapHand.Fingers[i].bones[j].NextJoint.ToVector3();

//                    var prevJointPosition = leapHand.Fingers[i].bones[j].PrevJoint.ToVector3();

//                    Vector3 localBonePosition = Quaternion.Inverse(leapHand.Fingers[i].bones[j].Rotation.ToQuaternion()) * (bonePosition - prevJointPosition);


//                    float positionDistance = Vector3.Distance(savedBonePositions[i * 4 + j], localBonePosition);

//                    boneErrors[i * 4 + j] = Mathf.Clamp01(positionDistance / maxDistance);

//                    capsuleHandColours[i * 4 + j] = Color.Lerp(Color.green, Color.red, boneErrors[i * 4 + j]);
//                }
//            }
//        }
//    }
//}
