using UnityEditor;

namespace Leap.Unity.ContactHands
{
    [CustomEditor(typeof(ContactManager))]
    public class ContactManagerEditor : CustomEditorBase<ContactManager>
    {
        private readonly string[] _testHandPoses = { "Hard Contact", "Soft Contact", "No Contact", "Custom" };

        protected override void OnEnable()
        {
            base.OnEnable();
            specifyConditionalDrawing(() => false, "editTimePose");
            specifyCustomDrawer("contactMode", DrawCustomEnum);
            SerializedProperty contactModeProperty = serializedObject.FindProperty("contactMode");
            specifyConditionalDrawing(() => contactModeProperty.enumValueIndex == ((int)ContactManager.ContactMode.Custom), "contactHands");
        }

        private void DrawCustomEnum(SerializedProperty property)
        {
            property.enumValueIndex = EditorGUILayout.Popup("Contact Mode", property.enumValueIndex, _testHandPoses);

            if(serializedObject.ApplyModifiedProperties())
            {
                target.SetContactMode((ContactManager.ContactMode)property.enumValueIndex);
            }
        }
    }
}