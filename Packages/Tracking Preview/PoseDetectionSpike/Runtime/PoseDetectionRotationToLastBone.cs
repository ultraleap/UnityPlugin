using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;

public class PoseDetectionRotationToLastBone : PoseDetectionBase
{
    public Vector3 eulerAnglesSavedRot;
    public Vector3 eulerAnglesCurrentRot;
    public Vector3 eulerAngles;

    public bool debugYaw = false;

    Color[] capsuleHandColours;

    public Quaternion[] savedBoneRotations;
    public Vector3[] savedBonePositions;
    public Vector3 savedWristPosition;
    public Quaternion savedWristRotation;

    // Start is called before the first frame update
    void Start()
    {
        capsuleHand.SetIndividualSphereColors = true;
        capsuleHandColours = capsuleHand.SphereColors;

        boneErrorPitch = new float[20];
        boneErrorYaw = new float[20];
    }

    // Update is called once per frame
    void Update()
    {
        Hand leapHand = capsuleHand.GetLeapHand();
        if (leapHand == null) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            savedBoneRotations = new Quaternion[32];
            savedBonePositions = new Vector3[20];
            savedWristPosition = leapHand.WristPosition;
            savedWristRotation = leapHand.Rotation;

            // loop through bones
            for (int i = 0; i < 5; i++)
            {
                Quaternion lastBoneRotation = leapHand.Rotation;
                for (int j = 0; j < 4; j++)
                {
                    Quaternion boneRotation = leapHand.Fingers[i].bones[j].Rotation;
                    savedBoneRotations[i * 4 + j] = Quaternion.Inverse(lastBoneRotation) * boneRotation;
                    lastBoneRotation = boneRotation;

                    savedBonePositions[i * 4 + j] = leapHand.Fingers[i].bones[j].NextJoint;
                }
            }
        }
        else if (savedBoneRotations != null && savedBoneRotations.Length > 0)
        {

            for (int i = 0; i < 5; i++)
            {
                Quaternion lastBoneRotation = leapHand.Rotation;
                for (int j = 0; j < 4; j++)
                {
                    Quaternion boneRotation = leapHand.Fingers[i].bones[j].Rotation;
                    Vector3 savedRotEuler = savedBoneRotations[i * 4 + j].eulerAngles;
                    Vector3 currentRotEuler = (Quaternion.Inverse(lastBoneRotation) * boneRotation).eulerAngles;

                    Vector3 eulerDifference = GetEulerAngleDifference(savedRotEuler, currentRotEuler);

                    //if(i == 1 && j == 1)
                    //{
                    //    eulerAnglesSavedRot = savedBoneRotations[i * 4 + j].eulerAngles;
                    //    eulerAnglesCurrentRot = (Quaternion.Inverse(lastBoneRotation) * boneRotation).eulerAngles;

                    //    eulerAngles = GetEulerAngleDifference(eulerAnglesCurrentRot, eulerAnglesSavedRot); //differenceToSavedRot.eulerAngles;

                    //    Debug.Log(eulerAngles);
                    //}

                    //float angle = Quaternion.Angle(savedBoneRotations[i * 4 + j], Quaternion.Inverse(lastBoneRotation) * boneRotation);
                    lastBoneRotation = boneRotation;

                    boneErrorPitch[i * 4 + j] = eulerDifference.x;
                    boneErrorYaw[i * 4 + j] = eulerDifference.y;

                    if (debugYaw) capsuleHandColours[i * 4 + j] = Color.Lerp(Color.green, Color.red, boneErrorYaw[i * 4 + j] / maxAngle);
                    else capsuleHandColours[i * 4 + j] = Color.Lerp(Color.green, Color.red, boneErrorPitch[i * 4 + j] / maxAngle);
                }
            }
        }
    }

    Vector3 GetEulerAngleDifference(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.DeltaAngle(a.x, b.x), Mathf.DeltaAngle(a.y, b.y), Mathf.DeltaAngle(a.z, b.z));
    }
}
