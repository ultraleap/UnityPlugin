using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Leap.Unity
{
#if UNITY_EDITOR
    [CustomEditor(typeof(UltraleapSettings))]
    public class UltraleapSettingsDrawer : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LeapSubSystemSection();
        }

        private void LeapSubSystemSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Leap XRHands Subsystem", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("The Leap XRHands Subsystem passes hand input directly from the Leap Service to XRHands. " +
                "If you are using OpenXR for hand input, do not enable this option. " +
                "Instead, use the Hand Tracking Subsystem option in the OpenXR XR Plug-in Management settings." +
                "\r\n\nThis option can not be toggled at runtime.", MessageType.Info, true); ;

            SerializedProperty property = this.serializedObject.FindProperty("leapSubsystemEnabled");

            bool editorBool = property.boolValue;
            property.boolValue = EditorGUILayout.ToggleLeft("Enable Leap XRHands Subsystem", editorBool);

            EditorGUILayout.Space(30);
            if(GUILayout.Button("Reset To Defaults"))
            {
                if(EditorUtility.DisplayDialog("Reset all settings", "This will reset all settings in this Ultraleap settings file", "Yes", "No"))
                {
                    var settingsTarget = target as UltraleapSettings;
                    settingsTarget.ResetToDefaults();
                }
            }

            this.serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    public class UltraleapSettings : ScriptableObject
    {
        [HideInInspector, SerializeField]
        public bool leapSubsystemEnabled;

        public void ResetToDefaults()
        {
            leapSubsystemEnabled = false;
        }

#if UNITY_EDITOR
        [MenuItem("Ultraleap/Open Ultraleap Settings")]
        private static void SelectULSettingsDropdown()
        {
            SelectUlSettings();
        }

        private static void SelectUlSettings(bool silent = false)
        {
            UltraleapSettings ulSettings = FindSettingsSO();
            if (ulSettings != null)
            {
                Selection.activeObject = ulSettings;
            }
            else
            {
                EditorUtility.DisplayDialog("No ultraleap settings file", "There is no settings file available, please try re-importing the plugin.", "Ok");
            }
        }
#endif

        public static UltraleapSettings FindSettingsSO()
        {
            UltraleapSettings[] settingsSO = Resources.FindObjectsOfTypeAll(typeof(UltraleapSettings)) as UltraleapSettings[];

            if (settingsSO != null && settingsSO.Length > 0)
            {
                return settingsSO[0]; // Assume there is only one settings file
            }
            else
            {
                return CreateSettingsSO();
            }
        }

        static UltraleapSettings CreateSettingsSO()
        {
#if UNITY_EDITOR
            UltraleapSettings newSO = ScriptableObject.CreateInstance<UltraleapSettings>();

            Directory.CreateDirectory(Application.dataPath + "/XR/");
            Directory.CreateDirectory(Application.dataPath + "/XR/Settings/");

            AssetDatabase.CreateAsset(newSO, "Assets/XR/Settings/Ultraleap Settings.asset");

            return newSO;
#endif
            return null;
        }
    }
}