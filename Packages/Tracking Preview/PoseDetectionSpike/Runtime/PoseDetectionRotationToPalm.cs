//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Leap;
//using Leap.Unity;
//using Leap.Unity.Encoding;


public class PoseDetectionRotationToPalm : PoseDetectionBase
{
//    Color[] capsuleHandColours;

//    Quaternion[] savedBoneRotations;

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
//            savedBoneRotations = new Quaternion[20];
//            Hand leapHand = capsuleHand.GetLeapHand();

//            Quaternion inversePalmRotation = Quaternion.Inverse(leapHand.Rotation.ToQuaternion());

//            // loop through bones
//            for (int i = 0; i < 5; i++)
//            {
//                for (int j = 0; j < 4; j++)
//                {
//                    Quaternion boneRotation = leapHand.Fingers[i].bones[j].Rotation.ToQuaternion();
//                    savedBoneRotations[i * 4 + j] = inversePalmRotation * boneRotation;
//                }
//            }
//        }
//        else if(savedBoneRotations != null)
//        {
//            Hand leapHand = capsuleHand.GetLeapHand();

//            Quaternion inversePalmRotation = Quaternion.Inverse(leapHand.Rotation.ToQuaternion());

//            for (int i = 0; i < 5; i++)
//            {
//                for (int j = 0; j < 4; j++)
//                {
//                    Quaternion boneRotation = leapHand.Fingers[i].bones[j].Rotation.ToQuaternion();
//                    float angle = Quaternion.Angle(savedBoneRotations[i * 4 + j], inversePalmRotation * boneRotation);

//                    boneErrors[i * 4 + j] = angle;

//                    capsuleHandColours[i * 4 + j] = Color.Lerp(Color.green, Color.red, boneErrors[i * 4 + j] / maxAngle);
//                }
//            }
//        }
//    }
}
