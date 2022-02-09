using Leap.Unity;
using UnityEditor;

namespace Ultraleap.Tracking.OpenXR
{

    [CustomEditor(typeof(OpenXRLeapProvider))]
    public class OpenXRLeapProviderEditor : CustomEditorBase
    {
        private readonly string[] _testHandPoses = { "HeadMountedA", "HeadMountedB" };

        protected override void OnEnable()
        {
            base.OnEnable();
            specifyCustomDrawer("editTimePose", DrawCustomEnum);
        }

        private void DrawCustomEnum(SerializedProperty property)
        {
            property.enumValueIndex = EditorGUILayout.Popup("Edit Time Pose", property.enumValueIndex, _testHandPoses);
            serializedObject.ApplyModifiedProperties();
        }

    }
}