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
            
            EditorGUILayout.HelpBox("If using OpenXR for hand input, use the Hand Tracking Subsystem in XR Plug-in Management/OpenXR. Do not enable both subsystems." +
                "\r\n\nThis option can not be toggled at runtime.", MessageType.Info, true);

            EditorGUILayout.Space(5);

            SerializedProperty leapSubsystemEnabledProperty = this.serializedObject.FindProperty("leapSubsystemEnabled");
            leapSubsystemEnabledProperty.boolValue = EditorGUILayout.ToggleLeft("Enable Leap XRHands Subsystem", leapSubsystemEnabledProperty.boolValue);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            SerializedProperty updateLeapInputSystemProperty = this.serializedObject.FindProperty("updateLeapInputSystem");
            updateLeapInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Leap Input System", updateLeapInputSystemProperty.boolValue);

            SerializedProperty updateMetaInputSystemProperty = this.serializedObject.FindProperty("updateMetaInputSystem");
            updateMetaInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Meta Aim Input System", updateMetaInputSystemProperty.boolValue);

            EditorGUILayout.Space(30);

            if (GUILayout.Button("Reset To Defaults"))
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
        static UltraleapSettings instance;
        public static UltraleapSettings Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                else
                    return instance = FindSettingsSO();
            }
            set { instance = value; }
        }

        [HideInInspector, SerializeField]
        public bool leapSubsystemEnabled;

        [HideInInspector, SerializeField]
        public bool updateLeapInputSystem;

        [HideInInspector, SerializeField]
        public bool updateMetaInputSystem;

        public void ResetToDefaults()
        {
            leapSubsystemEnabled = false;
            updateLeapInputSystem = false;
            updateMetaInputSystem = false;
        }

#if UNITY_EDITOR
        [MenuItem("Ultraleap/Open Ultraleap Settings")]
        private static void SelectULSettingsDropdown()
        {
            SelectUlSettings();
        }

        private static void SelectUlSettings(bool silent = false)
        {
            UltraleapSettings ulSettings = Instance;
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

        private static UltraleapSettings FindSettingsSO()
        {
            // Try to directly load the asset
            UltraleapSettings ultraleapSettings = Resources.Load<UltraleapSettings>("Ultraleap Settings");

            if (ultraleapSettings != null)
            {
                instance = ultraleapSettings;
                return instance;
            }

            UltraleapSettings[] settingsSO = Resources.FindObjectsOfTypeAll(typeof(UltraleapSettings)) as UltraleapSettings[];

            if (settingsSO != null && settingsSO.Length > 0)
            {
                instance = settingsSO[0]; // Assume there is only one settings file
            }
            else
            {
                instance = CreateSettingsSO();
            }

            return instance;
        }

        static UltraleapSettings CreateSettingsSO()
        {
            UltraleapSettings newSO = null;
#if UNITY_EDITOR
            newSO = ScriptableObject.CreateInstance<UltraleapSettings>();

            Directory.CreateDirectory(Application.dataPath + "/Resources/");

            AssetDatabase.CreateAsset(newSO, "Assets/Resources/Ultraleap Settings.asset");

#endif
            return newSO;
        }
    }
}