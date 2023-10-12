using UnityEditor;
using UnityEngine;

namespace Leap.Unity.PhysicsHands
{
    [CustomEditor(typeof(PhysicsHandsManager))]
    public class PhysicsHandsManagerEditor : CustomEditorBase<PhysicsHandsManager>
    {
        bool layersExist = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            specifyConditionalDrawing(() => false, "editTimePose");
            specifyConditionalDrawing(() => target.ContactMode == PhysicsHandsManager.ContactModes.Custom, "contactParent");

            if(CreateContactHandLayers())
            { 
                layersExist = true;
            }
        }

        public override void OnInspectorGUI()
        {
            WarningsSection();
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();
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
            if (CreateLayer("ContactHands") && CreateLayer("ContactHandsReset"))
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
                        UnityEngine.Debug.Log("Layer: " + layerName + " has been added for Contact Hands");
                        tagManager.ApplyModifiedProperties();
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }

            UnityEngine.Debug.LogWarning("All allowed layers have been filled. Contact Hands requires 2 available layers to function.", target.gameObject);

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