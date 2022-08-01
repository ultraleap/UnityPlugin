using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HandShape))]
public class HandShapeEditor : Editor
{
    HandShape handShape;
    int selectedJointIdx = -1;

    int thresholdControlSelected = -1; // -1 = none; 0 = yaw; 1 = pitch
    float thresholdControlDistanceToCentre;
    Vector3 thresholdControlArcCentre;
    float thresholdControlSavedStartingThreshold;

    // Start is called before the first frame update
    void Start()
    {
        handShape = target as HandShape;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnSceneGUI()
    {
        //Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        EventType eventType = Event.current.type;
        Debug.Log(eventType);

        // this makes sure that the HandShape gameobject stays selected, even if you click somewhere in the scene view
        if (eventType == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(0);
        }

        if (handShape == null) handShape = target as HandShape;

        if (handShape.poseDetectionRecording != null && handShape.poseDetectionCombineErrors != null)
        {
            
            Quaternion[] boneRotations = handShape.poseDetectionRecording.savedBoneRotations;
            if (boneRotations != null && boneRotations.Length > 0)
            {
                Vector3[] jointPositions = JointPositionsFromRotations(boneRotations);
                // the selectedJointPosition is where the rotational handle and the threshold controls are drawn
                Vector3 selectedJointPosition = Vector3.zero;

                // draw rotational handle or threshold edit tools (if a joint is selected)
                if (selectedJointIdx != -1)
                {
                    if (selectedJointIdx % 4 != 0)
                    {
                        selectedJointPosition = jointPositions[selectedJointIdx - 1];
                    }

                    Quaternion lastBoneRotation = handShape.poseDetectionRecording.savedWristRotation;
                    for (int i = selectedJointIdx / 4 * 4; i < selectedJointIdx; i++)
                    {
                        lastBoneRotation *= boneRotations[i];
                    }

                    if (handShape.currentEditorMode == HandShape.HandShapeEditorModes.rotateBones)
                    {
                        EditorGUI.BeginChangeCheck();
                        // draw handle that allows bone rotations
                        Quaternion tempRotation = Quaternion.Normalize(Quaternion.Inverse(lastBoneRotation) * Handles.RotationHandle(lastBoneRotation * boneRotations[selectedJointIdx], selectedJointPosition));
                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(handShape.poseDetectionRecording, "Rotated Bone");
                            boneRotations[selectedJointIdx] = tempRotation;
                        }
                    }

                    if (handShape.currentEditorMode == HandShape.HandShapeEditorModes.editThresholds)
                    {
                        // draw handles that allows threshold changes
                        float[] thresholdsYaw = handShape.poseDetectionCombineErrors.thresholdsYaw;
                        float[] thresholdsPitch = handShape.poseDetectionCombineErrors.thresholdsPitch;
                        Handles.color = Color.white;
                        // Yaw arc
                        Handles.DrawWireArc(selectedJointPosition,
                            lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.up,
                            Vector3.Normalize(lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.Lerp(Vector3.forward, Vector3.left, thresholdsYaw[selectedJointIdx] / 180f)),
                            thresholdsYaw[selectedJointIdx],
                            HandleUtility.GetHandleSize(selectedJointPosition));
                        // Pitch arc
                        Handles.DrawWireArc(selectedJointPosition,
                            lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.left,
                            Vector3.Normalize(lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.Lerp(Vector3.forward, Vector3.down, thresholdsPitch[selectedJointIdx] / 180f)),
                            thresholdsPitch[selectedJointIdx],
                            HandleUtility.GetHandleSize(selectedJointPosition));

                        // yaw handle to edit thresholds
                        Handles.color = Color.blue;
                        Vector3 handlePosLeftYaw = Vector3.Normalize(lastBoneRotation * boneRotations[selectedJointIdx]
                                            * Vector3.Lerp(Vector3.forward, Vector3.left, thresholdsYaw[selectedJointIdx] / 180f))
                                            * HandleUtility.GetHandleSize(selectedJointPosition)
                                            + selectedJointPosition;
                        Handles.SphereHandleCap(0,
                            handlePosLeftYaw,
                            Quaternion.identity,
                            //Quaternion.FromToRotation(rotationalHandlePosition, handlePosLeft),
                            HandleUtility.GetHandleSize(handlePosLeftYaw) * 0.1f,
                            EventType.Repaint);
                        Handles.DrawLine(handlePosLeftYaw, selectedJointPosition);

                        Vector3 handlePosRightYaw = Vector3.Normalize(lastBoneRotation * boneRotations[selectedJointIdx]
                                            * Vector3.Lerp(Vector3.forward, Vector3.right, thresholdsYaw[selectedJointIdx] / 180f))
                                            * HandleUtility.GetHandleSize(selectedJointPosition)
                                            + selectedJointPosition;
                        Handles.SphereHandleCap(0,
                            handlePosRightYaw,
                            Quaternion.identity,
                            //Quaternion.FromToRotation(rotationalHandlePosition, handlePosRight),
                            HandleUtility.GetHandleSize(handlePosRightYaw) * 0.1f,
                            EventType.Repaint);
                        Handles.DrawLine(handlePosRightYaw, selectedJointPosition);

                        // pitch handle to edit thresholds
                        Handles.color = Color.green;
                        Vector3 handlePosLeftPitch = Vector3.Normalize(lastBoneRotation * boneRotations[selectedJointIdx]
                                            * Vector3.Lerp(Vector3.forward, Vector3.down, thresholdsPitch[selectedJointIdx] / 180f))
                                            * HandleUtility.GetHandleSize(selectedJointPosition)
                                            + selectedJointPosition;
                        Handles.SphereHandleCap(0,
                            handlePosLeftPitch,
                            Quaternion.identity,
                            //Quaternion.FromToRotation(rotationalHandlePosition, handlePosLeft),
                            HandleUtility.GetHandleSize(handlePosLeftPitch) * 0.1f,
                            EventType.Repaint);
                        Handles.DrawLine(handlePosLeftPitch, selectedJointPosition);

                        Vector3 handlePosRightPitch = Vector3.Normalize(lastBoneRotation * boneRotations[selectedJointIdx]
                                            * Vector3.Lerp(Vector3.forward, Vector3.up, thresholdsPitch[selectedJointIdx] / 180f))
                                            * HandleUtility.GetHandleSize(selectedJointPosition)
                                            + selectedJointPosition;
                        Handles.SphereHandleCap(0,
                            handlePosRightPitch,
                            Quaternion.identity,
                            //Quaternion.FromToRotation(rotationalHandlePosition, handlePosRight),
                            HandleUtility.GetHandleSize(handlePosRightPitch) * 0.1f,
                            EventType.Repaint);
                        Handles.DrawLine(handlePosRightPitch, selectedJointPosition);

                        if (eventType == EventType.MouseDown && Event.current.button == 0) // right click
                        {
                            // get closest sphere:
                            float distanceToPitchHandles = Mathf.Min(HandleUtility.DistanceToCircle(handlePosLeftPitch, 0f),
                                HandleUtility.DistanceToCircle(handlePosRightPitch, 0f));
                            float distanceToYawHandles = Mathf.Min(HandleUtility.DistanceToCircle(handlePosLeftYaw, 0f),
                                HandleUtility.DistanceToCircle(handlePosRightYaw, 0f));
                            Debug.Log("min: " + Mathf.Min(distanceToPitchHandles, distanceToYawHandles));
                            if (Mathf.Min(distanceToPitchHandles, distanceToYawHandles) < 5f)
                            {
                                thresholdControlArcCentre = selectedJointPosition
                                    + lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.forward
                                    * HandleUtility.GetHandleSize(selectedJointPosition);

                                if (distanceToPitchHandles < distanceToYawHandles)
                                {
                                    thresholdControlSelected = 1;
                                    thresholdControlDistanceToCentre = Vector3.Distance(thresholdControlArcCentre, handlePosLeftPitch);
                                    thresholdControlSavedStartingThreshold = thresholdsPitch[selectedJointIdx];
                                }
                                else
                                {
                                    thresholdControlSelected = 0;
                                    thresholdControlDistanceToCentre = Vector3.Distance(thresholdControlArcCentre, handlePosLeftYaw);
                                    thresholdControlSavedStartingThreshold = thresholdsYaw[selectedJointIdx];
                                }
                            }
                        }
                        else if (eventType == EventType.MouseUp)
                        {
                            thresholdControlSelected = -1;
                        }

                        // make spheres draggable:
                        if(eventType == EventType.MouseDrag && thresholdControlSelected != -1)
                        {
                            // pitch
                            if(thresholdControlSelected == 1)
                            {
                                Vector3 closestPointOnArc = HandleUtility.ClosestPointToArc(selectedJointPosition,
                                    lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.left,
                                    lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.down,
                                    180,
                                    HandleUtility.GetHandleSize(selectedJointPosition));

                                Undo.RecordObject(handShape.poseDetectionCombineErrors, "changed Thresholds");

                                thresholdsPitch[selectedJointIdx] = Mathf.Clamp(Vector3.Distance(closestPointOnArc, thresholdControlArcCentre)
                                    / thresholdControlDistanceToCentre
                                    * thresholdControlSavedStartingThreshold,
                                    0, 360);
                            }
                            // yaw
                            else if (thresholdControlSelected == 0)
                            {
                                Vector3 closestPointOnArc = HandleUtility.ClosestPointToArc(selectedJointPosition,
                                    lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.up,
                                    lastBoneRotation * boneRotations[selectedJointIdx] * Vector3.left,
                                    180,
                                    HandleUtility.GetHandleSize(selectedJointPosition));

                                Undo.RecordObject(handShape.poseDetectionCombineErrors, "changed Thresholds");

                                thresholdsYaw[selectedJointIdx] = Mathf.Clamp(Vector3.Distance(closestPointOnArc, thresholdControlArcCentre)
                                    / thresholdControlDistanceToCentre
                                    * thresholdControlSavedStartingThreshold,
                                    0, 360);
                            }
                        }
                    }


                }

                // if mouse clicked, find index of the joint that is closest to the click
                if (eventType == EventType.MouseDown && Event.current.button == 0) // right click
                {
                    Debug.Log(HandleUtility.DistanceToCircle(selectedJointPosition, 0f));
                    // if user is not currently trying to interact with the rotational handle
                    if (selectedJointIdx == -1 || HandleUtility.DistanceToCircle(selectedJointPosition, 0f) > 90f)
                    {
                        selectedJointIdx = -1;
                        float smallestDistance = float.MaxValue;
                        for (int i = 0; i < jointPositions.Length; i++)
                        {
                            float d = float.MaxValue;

                            if (i % 4 == 0) d = HandleUtility.DistanceToLine(Vector3.zero, jointPositions[i]);
                            else d = HandleUtility.DistanceToLine(jointPositions[i - 1], jointPositions[i]);

                            if (d < smallestDistance && d < 10f)
                            {
                                smallestDistance = d;
                                selectedJointIdx = i;
                            }
                        }
                    }
                }

                


                for (int i = 0; i < jointPositions.Length; i++)
                {
                    // draw joints
                    Handles.color = Color.white;
                    Handles.SphereHandleCap(0, jointPositions[i], Quaternion.identity, 0.01f, EventType.Repaint);

                    // draw bones as lines
                    Handles.color = Color.black;
                    float lineThickness = 0f;
                    if (i == selectedJointIdx)
                    {
                        Handles.color = Color.grey;
                        lineThickness = 3f;
                    }
                    if (i % 4 != 0)
                    {
                        Handles.DrawLine(jointPositions[i - 1], jointPositions[i], lineThickness);
                    }
                    else
                    {
                        Handles.DrawLine(Vector3.zero, jointPositions[i], lineThickness);
                    }
                }
            }



            // draw bone positions
            //Vector3[] bonePositions = handShape.poseDetectionRecording.savedBonePositions;
            //if (bonePositions != null && bonePositions.Length > 0)
            //{
            //    for (int i = 0; i < bonePositions.Length; i++)
            //    {
            //        Handles.color = Color.Lerp(Color.green, Color.red, (float)i / bonePositions.Length);
            //        Handles.SphereHandleCap(0, bonePositions[i] - handShape.poseDetectionRecording.savedWristPosition, Quaternion.identity, 0.01f, EventType.Repaint);
            //    }
            //    Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, 0.01f, EventType.Repaint);
            //}
        }

        if (EditorGUI.EndChangeCheck())
        {
            //position = newPosition;
        }
    }

    public Vector3[] actualJointPositions;
    public Vector3 actualWristPos;
    public Quaternion actualWristRot;

    // function to calculate the joint positions from rotations:
    Vector3[] JointPositionsFromRotations(Quaternion[] rotations)
    {
        if (actualJointPositions == null || actualWristPos == null || actualWristRot == null)
        {
            actualJointPositions = handShape.poseDetectionRecording.savedBonePositions;
            actualWristPos = handShape.poseDetectionRecording.savedWristPosition;
            actualWristRot = handShape.poseDetectionRecording.savedWristRotation;
        }


        Vector3 wristPos = Vector3.zero;
        Quaternion wristRot = actualWristRot;

        Vector3[] jointPositions = new Vector3[rotations.Length];

        for (int i = 0; i < 5; i++)
        {
            Vector3 lastPos = wristPos;
            Quaternion lastRot = wristRot;

            for (int j = 0; j < 4; j++)
            {
                //if (i == 0 && j == 0) continue;

                float boneLength = 0;
                if(j == 0)
                {
                    boneLength = Vector3.Distance(actualJointPositions[i * 4 + j], actualWristPos);
                }
                else
                {
                    boneLength = Vector3.Distance(actualJointPositions[i * 4 + j], actualJointPositions[i * 4 + j - 1]);
                }

                jointPositions[i * 4 + j] = lastPos + lastRot * rotations[i * 4 + j] * Vector3.forward * boneLength;
                lastPos = jointPositions[i * 4 + j];
                lastRot = lastRot * rotations[i * 4 + j];
            }
        }

        return jointPositions;
    }
}
