using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Leap.Unity.HandsModule
{
    [CustomEditor(typeof(HandPoseRecorder))]
    public class HandPoseRecoderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HandPoseRecorder myScript = (HandPoseRecorder)target;
            if (GUILayout.Button("Save Current Hand Pose"))
            {
                Debug.Log("Button Called");
                myScript.SaveCurrentHandPose();
            }
        }
    }


}