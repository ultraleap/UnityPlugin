using UnityEditor;
using UnityEngine;

namespace Leap.PhysicalHands
{
    [CustomEditor(typeof(PhysicalHandsManager))]
    public class PhysicalHandsManagerEditor : CustomEditorBase<PhysicalHandsManager>
    {
        private readonly string[] contactModeNames = { "Hard Contact", "Soft Contact", "No Contact" };

        bool layersExist = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            specifyConditionalDrawing(() => false, "editTimePose");

            specifyCustomDrawer("_contactMode", DrawCustomEnum);

            HandleEventsFoldout();

            if (CreateContactHandLayers())
            {
                layersExist = true;
            }
        }

        public override void OnInspectorGUI()
        {
            WarningsSection();
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

            if (!PhysicalHandsSettings.AllSettingsApplied())
            {
                EditorGUILayout.Space(10);

                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("One or more of your project settings are not recommended when using Physical Hands.", MessageType.Warning);
                if (GUILayout.Button("View Recommended Settings", GUILayout.Height(37)))
                {
                    SettingsService.OpenProjectSettings("Project/Ultraleap/Physical Hands");
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawCustomEnum(SerializedProperty property)
        {
            property.enumValueIndex = EditorGUILayout.Popup("Contact Mode", property.enumValueIndex, contactModeNames);

            if (serializedObject.ApplyModifiedProperties())
            {
                target.SetContactMode((PhysicalHandsManager.ContactMode)property.enumValueIndex);
            }
        }

        private void HandleEventsFoldout()
        {
            addPropertyToFoldout("onHover", "Events");
            addPropertyToFoldout("onHoverExit", "Events");
            addPropertyToFoldout("onContact", "Events");
            addPropertyToFoldout("onContactExit", "Events");
            addPropertyToFoldout("onGrab", "Events");
            addPropertyToFoldout("onGrabExit", "Events");
        }

        void WarningsSection()
        {
            if (!layersExist)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"All allowed layers have been filled. Contact Hands requires 2 available layers to function. \n Delete 2 layers to use Contact Hands.", MessageType.Warning);
                if (GUILayout.Button("Edit Layers", GUILayout.Width(80)))
                {
                    SettingsService.OpenProjectSettings("Project/Tags and Layers");
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        bool CreateContactHandLayers()
        {
            if (CreateLayer("PhysicalHands") && CreateLayer("PhysicalHandsReset"))
            {
                return true;
            }

            return false;
        }

        private bool CreateLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (!PropertyExists(layersProp, 0, 32, layerName))
            {
                SerializedProperty sp;
                for (int i = 8; i < 32; i++)
                {
                    sp = layersProp.GetArrayElementAtIndex(i);
                    if (sp.stringValue == "")
                    {
                        sp.stringValue = layerName;
                        UnityEngine.Debug.Log("Layer: " + layerName + " has been added for Physical Hands");
                        tagManager.ApplyModifiedProperties();
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }

            UnityEngine.Debug.LogWarning("All allowed layers have been filled. Physical Hands requires 2 available layers to function.", target.gameObject);

            return false;
        }

        private bool PropertyExists(SerializedProperty property, int start, int end, string value)
        {
            for (int i = start; i < end; i++)
            {
                SerializedProperty t = property.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}