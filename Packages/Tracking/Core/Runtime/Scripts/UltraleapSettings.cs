using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Leap.Unity
{
#if UNITY_EDITOR
    static class UltraleapProjectSettings
    {
        [SettingsProvider]
        public static SettingsProvider CreateUltraleapSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/Ultraleap", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Ultraleap",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    LeapSubSystemSection();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "XRHands", "Leap", "Leap Input System", "Meta Aim System", "Subsystem" })
            };

            return provider;
        }

        private static void LeapSubSystemSection()
        {
            var settings = UltraleapSettings.GetSerializedSettings();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Leap XRHands Subsystem", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("If using OpenXR for hand input, use the Hand Tracking Subsystem in XR Plug-in Management/OpenXR. Do not enable both subsystems." +
                "\r\n\nThis option can not be toggled at runtime.", MessageType.Info, true);

            EditorGUILayout.Space(5);

            SerializedProperty leapSubsystemEnabledProperty = settings.FindProperty("leapSubsystemEnabled");
            leapSubsystemEnabledProperty.boolValue = EditorGUILayout.ToggleLeft("Enable Leap XRHands Subsystem", leapSubsystemEnabledProperty.boolValue);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            SerializedProperty updateLeapInputSystemProperty = settings.FindProperty("updateLeapInputSystem");
            updateLeapInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Leap Input System", updateLeapInputSystemProperty.boolValue);

            SerializedProperty updateMetaInputSystemProperty = settings.FindProperty("updateMetaInputSystem");
            updateMetaInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Meta Aim Input System", updateMetaInputSystemProperty.boolValue);

            EditorGUILayout.Space(30);

            if (GUILayout.Button("Reset To Defaults"))
            {
                if(EditorUtility.DisplayDialog("Reset all settings", "This will reset all settings in this Ultraleap settings file", "Yes", "No"))
                {
                    UltraleapSettings.Instance.ResetToDefaults();
                }
            }

            settings.ApplyModifiedPropertiesWithoutUndo();

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
            SettingsService.OpenProjectSettings("Project/Ultraleap");
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

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(FindSettingsSO());
        }
    }
}