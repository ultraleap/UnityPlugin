using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(HandPoseRecorder))]
public class HandPoseSnapRecorder : MonoBehaviour
{
    public HandPoseViewer poseViewerPrefab;
    public Transform transformToSnapTo;

    HandPoseRecorder handPoseRecorder;

    private void Awake()
    {
        handPoseRecorder = GetComponent<HandPoseRecorder>();

        if (transformToSnapTo == null)
            transformToSnapTo = transform;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            handPoseRecorder.savePath = "HandPoses/PoseSnapping/" + transformToSnapTo.name + "/";
            //handPoseRecorder.handPoseName = transformToSnapTo.name + " Pose";

            HandPoseScriptableObject newPose = handPoseRecorder.SaveCurrentHandPose();

            if (newPose == null) // no hand pose could be saved, we can not continue
                return;

            HandPoseSnapScriptableObject newPoseSnap = ScriptableObject.CreateInstance<HandPoseSnapScriptableObject>();
            newPoseSnap.name = newPose.name + " Snap";
            newPoseSnap.chirality = handPoseRecorder.handToRecord;
            newPoseSnap.handPose = newPose;

            var positionOffset = transformToSnapTo.InverseTransformPoint(transformToSnapTo.position) - transformToSnapTo.InverseTransformPoint(Hands.Provider.GetHand(handPoseRecorder.handToRecord).GetPalmPose().position);
            var rotationOffset = Quaternion.Inverse(Hands.Provider.GetHand(handPoseRecorder.handToRecord).GetPalmPose().rotation) * transformToSnapTo.rotation;

            newPoseSnap.poseToObjectOffset = new Pose(positionOffset, rotationOffset);

            SaveHandPoseSnap(newPoseSnap);

            if (poseViewerPrefab != null)
            {
                var newViewer = Instantiate(poseViewerPrefab, Hands.Provider.GetHand(handPoseRecorder.handToRecord).GetPalmPose().position, Hands.Provider.GetHand(handPoseRecorder.handToRecord).GetPalmPose().rotation);
                newViewer.handPose = newPoseSnap.handPose;
            }
        }
    }

    void SaveHandPoseSnap(HandPoseSnapScriptableObject newItem)
    {
#if UNITY_EDITOR
        if (!Directory.Exists("Assets/" + handPoseRecorder.savePath))
        {
            Directory.CreateDirectory("Assets/" + handPoseRecorder.savePath);
        }

        string fullPath = "Assets/" + handPoseRecorder.savePath + newItem.name + ".asset";

        int fileIterator = 1;
        while (File.Exists(fullPath))
        {
            fullPath = "Assets/" + handPoseRecorder.savePath + newItem.name + " (" + fileIterator + ")" + ".asset";
            fileIterator++;
        }

        AssetDatabase.CreateAsset(newItem, fullPath);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(fullPath);
#endif
    }
}

public static class PoseUtils
{
    public static Pose Diff(this Pose to, Pose from)
    {
        to.position = to.position - from.position;
        to.rotation = to.rotation.Diff(from.rotation);
        return to;
    }

    public static Quaternion Diff(this Quaternion to, Quaternion from)
    {
        return to * Quaternion.Inverse(from);
    }
}