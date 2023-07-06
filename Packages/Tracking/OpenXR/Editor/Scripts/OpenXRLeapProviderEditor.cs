using Leap.Unity;
using UnityEditor;
using UnityEngine;

namespace Ultraleap.Tracking.OpenXR
{
    [CustomEditor(typeof(OpenXRLeapProvider))]
    public class OpenXRLeapProviderEditor : CustomEditorBase
    {
        private readonly string[] _testHandPoses = { "HeadMountedA", "HeadMountedB" };
        private SerializedProperty _mainCamera;

        protected override void OnEnable()
        {
            base.OnEnable();

            _mainCamera = serializedObject.FindProperty("_mainCamera");
            if (_mainCamera.objectReferenceValue == null)
            {
                _mainCamera.objectReferenceValue = Camera.main;
                serializedObject.ApplyModifiedProperties();

                if (_mainCamera.objectReferenceValue != null)
                {
                    Debug.Log("Camera.Main automatically assigned");
                }
            }

            specifyCustomDrawer("editTimePose", DrawCustomEnum);
        }

        private void DrawCustomEnum(SerializedProperty property)
        {
            property.enumValueIndex = EditorGUILayout.Popup("Edit Time Pose", property.enumValueIndex, _testHandPoses);
            serializedObject.ApplyModifiedProperties();
        }
    }
}