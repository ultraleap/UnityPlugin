/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Leap
{
#if UNITY_EDITOR
    [CustomEditor(typeof(UltraleapSettings))]
    public class UltraleapSettingsDrawer : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(20);
            if (GUILayout.Button("Open Ultraleap Settings"))
            {
                SettingsService.OpenProjectSettings("Project/Ultraleap");
            }
        }
    }
#endif

#if UNITY_EDITOR
    static class UltraleapProjectSettings
    {
        static SerializedObject settings = UltraleapSettings.GetSerializedSettings();

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
                    if (settings == null)
                    {
                        settings = UltraleapSettings.GetSerializedSettings();
                    }

                    LeapSubSystemSection(settings);
                    InputActionsSection(settings);
                    HintingSection(settings);
                    NotificationSection(settings);
                    RenderPipelineSupportSection(settings); 
                    ResetSection(settings);

                    settings.ApplyModifiedProperties();
                    settings.UpdateIfRequiredOrScript();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "XRHands", "Leap", "Leap Input System", "Meta Aim System", "Subsystem", "Leap Motion", "Ultraleap", "Ultraleap Settings" })
            };

            return provider;
        }

        private static void LeapSubSystemSection(SerializedObject settings)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Leap XRHands Subsystem", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("If using OpenXR for hand input, use the Hand Tracking Subsystem in XR Plug-in Management/OpenXR. Do not enable both subsystems." +
                "\r\n\nThis option can not be toggled at runtime.", MessageType.Info, true);

            EditorGUILayout.Space(5);

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty leapSubsystemEnabledProperty = settings.FindProperty("leapSubsystemEnabled");
                leapSubsystemEnabledProperty.boolValue = EditorGUILayout.ToggleLeft("Enable Leap XRHands Subsystem", leapSubsystemEnabledProperty.boolValue);
            }

            EditorGUILayout.Space(30);

            settings.ApplyModifiedProperties();
        }

        private static void InputActionsSection(SerializedObject settings)
        {
            EditorGUILayout.LabelField("Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty updateLeapInputSystemProperty = settings.FindProperty("updateLeapInputSystem");
                updateLeapInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Leap Input System with XRHands", updateLeapInputSystemProperty.boolValue);

                SerializedProperty updateMetaInputSystemProperty = settings.FindProperty("updateMetaInputSystem");
                updateMetaInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Meta Aim Input System with XRHands", updateMetaInputSystemProperty.boolValue);
            }

            EditorGUILayout.Space(30);

            settings.ApplyModifiedProperties();
        }

        private static void HintingSection(SerializedObject settings)
        {
            EditorGUILayout.LabelField("Hand Tracking Hints", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty hints = settings.FindProperty("startupHints");
                EditorGUILayout.PropertyField(hints, true);
            }

            EditorGUILayout.Space(10);

            settings.ApplyModifiedProperties();
        }

        private static void NotificationSection(SerializedObject settings)
        {
            EditorGUILayout.LabelField("Notifications", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            using (new EditorGUI.IndentLevelScope())
            {
                // Android Build Warnings
                SerializedProperty showAndroidBuildArchitectureWarning = settings.FindProperty("showAndroidBuildArchitectureWarning");
                showAndroidBuildArchitectureWarning.boolValue = EditorGUILayout.ToggleLeft("Show Android Architecture build warning", showAndroidBuildArchitectureWarning.boolValue);

                // Physical Hands Settings Warnings
                SerializedProperty showPhysicalHandsPhysicsSettingsWarning = settings.FindProperty("showPhysicalHandsPhysicsSettingsWarning");
                showPhysicalHandsPhysicsSettingsWarning.boolValue = EditorGUILayout.ToggleLeft("Show Physical Hands settings warning", showPhysicalHandsPhysicsSettingsWarning.boolValue);

                // Attachment Hands delete content warnings
                bool curValue = !EditorUtility.GetDialogOptOutDecision(DialogOptOutDecisionType.ForThisMachine, "UL attachment hands popup");
                curValue = EditorGUILayout.ToggleLeft("Show clear Attachment Hands deletes content warning", curValue);
                EditorUtility.SetDialogOptOutDecision(DialogOptOutDecisionType.ForThisMachine, "UL attachment hands popup", !curValue);
            }

            EditorGUILayout.Space(30);

            settings.ApplyModifiedProperties();
        }


        private static void RenderPipelineSupportSection(SerializedObject settings)
        {
            EditorGUILayout.LabelField("Render Pipeline Support", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            using (new EditorGUI.IndentLevelScope())
            {
                // Enable automatic upgrades
                UltraleapSettings.AutomaticallyUpgradeMaterialsToCurrentRenderPipeline = 
                    EditorGUILayout.ToggleLeft("Enable automatic updates of the Ultraleap plugin materials to the active render pipeline on this machine.", UltraleapSettings.AutomaticallyUpgradeMaterialsToCurrentRenderPipeline);
                EditorGUILayout.TextArea("Note: if set to false, this setting will persist across ALL Unity projects that use the plugin. You can still manually force the upgrade.");
            }

            EditorGUILayout.Space(30);

            settings.ApplyModifiedProperties();
        }

        private static void ResetSection(SerializedObject settings)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset To Defaults", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth/2)))
            {
                if (EditorUtility.DisplayDialog("Reset all settings", "This will reset all settings in this Ultraleap settings file", "Yes", "No"))
                {
                    UltraleapSettings.Instance.ResetToDefaults();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            settings.ApplyModifiedProperties();
        }
    }
#endif

    public class UltraleapSettings : ScriptableObject
    {
        private static readonly bool defaultValueForAutomaticallyUpgradingMaterialsForActiveRenderPipeline = true;

        private static readonly string automaticallyUpgradeMaterialsToCurrentRenderPipelineEnvironmentVariableName = "ULTRALEAP_UNITY_PLUGIN_AUTOMATICALLY_UPGRADE_MATERIALS_TO_CURRENT_RENDER_PIPELINE";
       
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

        // XRHands and Input System
        [HideInInspector, SerializeField]
        public bool leapSubsystemEnabled = false;

        [HideInInspector, SerializeField]
        public bool updateLeapInputSystem = false;

        [HideInInspector, SerializeField]
        public bool updateMetaInputSystem = false;

        [HideInInspector, SerializeField]
        public string[] startupHints = new string[] { };

        [HideInInspector, SerializeField]
        public bool showAndroidBuildArchitectureWarning = true;

        [HideInInspector, SerializeField]
        public bool showPhysicalHandsPhysicsSettingsWarning = true;

        [HideInInspector]
        public static bool AutomaticallyUpgradeMaterialsToCurrentRenderPipeline
        {
            get
            {
                string readValue = System.Environment.GetEnvironmentVariable(automaticallyUpgradeMaterialsToCurrentRenderPipelineEnvironmentVariableName, EnvironmentVariableTarget.User);

                if (string.IsNullOrEmpty(readValue))
                {
                    AutomaticallyUpgradeMaterialsToCurrentRenderPipeline = defaultValueForAutomaticallyUpgradingMaterialsForActiveRenderPipeline;
                    return defaultValueForAutomaticallyUpgradingMaterialsForActiveRenderPipeline;
                }

                return Convert.ToBoolean(readValue);
            }

            set
            {
                System.Environment.SetEnvironmentVariable(automaticallyUpgradeMaterialsToCurrentRenderPipelineEnvironmentVariableName, Convert.ToString(value), EnvironmentVariableTarget.User);
            }
        }

        [HideInInspector, SerializeField]
        public string pluginVersion = "Unknown";
        public string PluginVersion
        {
            get
            {
#if UNITY_EDITOR
                pluginVersion = GetPluginVersion();
#endif
                return pluginVersion;
            }
        }

        [HideInInspector, SerializeField]
        public string pluginSource = "Unknown";
        public string PluginSource
        {
            get
            {
#if UNITY_EDITOR
                pluginSource = GetPluginSource();
#endif
                return pluginSource;
            }
        }

        public void ResetToDefaults()
        {
            leapSubsystemEnabled = false;
            updateLeapInputSystem = false;
            updateMetaInputSystem = false;

            startupHints = new string[] { };

            showAndroidBuildArchitectureWarning = true;
            showPhysicalHandsPhysicsSettingsWarning = true;

            AutomaticallyUpgradeMaterialsToCurrentRenderPipeline = defaultValueForAutomaticallyUpgradingMaterialsForActiveRenderPipeline;
        }

#if UNITY_EDITOR

        static string GetPluginVersion()
        {
            string version = "Unknown";

            if (Utils.IsPackageAvailable("com.ultraleap.tracking", out var packageInfo)) // Check the package exists so we can use package manage wizardry
            {
                version = packageInfo.version;
            }
            else // We are not using package manager :( we need to look for version number elsewhere
            {
                version = FindPluginVersionInAssets();
            }

            // Parse through system.version to improve formatting standardisation
            if(Version.TryParse(version, out var ver))
            {
                version = ver.ToString();
            }

            return version;
        }

        static string GetPluginSource()
        {
            if (Utils.IsPackageAvailable("com.ultraleap.tracking", out var packageInfo)) // Check the package exists so we can use package manage wizardry
            {
                if (packageInfo.source == UnityEditor.PackageManager.PackageSource.Registry &&
                    packageInfo.registry != null &&
                    packageInfo.registry.url.Contains("openupm"))
                {
                    return "UPM OpenUPM";
                }
                else
                {
                    return "UPM " + packageInfo.source;
                }
            }
            else
            {
                if(FindPluginVersionInAssets() != "")
                {
                    return "Unity Package";
                }
            }

            return "Unknown";
        }

        [MenuItem("Ultraleap/Open Ultraleap Settings", false, 50)]
        private static void SelectULSettingsDropdown()
        {
            SettingsService.OpenProjectSettings("Project/Ultraleap");
        }

        [MenuItem("Ultraleap/Help/Documentation", false, 100)]
        private static void OpenDocs()
        {
            Application.OpenURL("https://docs.ultraleap.com/unity-api/");
        }

        [MenuItem("Ultraleap/Help/Report a Bug", false, 101)]
        private static void OpenGithubNewIssueView()
        {
            Application.OpenURL("https://github.com/ultraleap/UnityPlugin/issues/new");
        }

        [MenuItem("Ultraleap/Help/Support/Submit a Support Request", false, 102)]
        private static void OpenSupportRequest()
        {
            Application.OpenURL("https://support.leapmotion.com/hc/en-us/requests/new");
        }

        [MenuItem("Ultraleap/Help/Support/Join Our Discord", false, 103)]
        private static void OpenDiscord()
        {
            Application.OpenURL("https://discord.com/invite/3VCndThqxS");
        }

        [MenuItem("Ultraleap/Updating/6.X to 7.X/Update Existing Pose Recordings", false, 200)]
        private static void UpdateExistingPoseRecordings()
        {
            ReplaceUseOfFingersInPoseScriptableObjects();
        }

        [MenuItem("Ultraleap/Updating/Update Materials for Active Render Pipeline", false, 201)]
        private static void UpdateMaterialsForActiveRenderPipeline()
        {
            AutomaticRenderPipelineMaterialShaderUpdater _materialUpdater = null;
                
            try
            {
                _materialUpdater = Resources.Load<AutomaticRenderPipelineMaterialShaderUpdater>("AutomaticRenderPipelineMaterialShaderUpdater");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            if (_materialUpdater != null)
            {
                _materialUpdater.AutoRefreshMaterialShadersForPipeline(silentMode: true);
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

#if UNITY_EDITOR
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(Instance);
        }

        static string FindPluginVersionInAssets()
        {
            string pluginVersionFromAssets = "";

            string[] fileGUIDs = AssetDatabase.FindAssets("Version");

            foreach (var guid in fileGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.Contains("/Ultraleap/Tracking/Version.txt"))
                {
                    string content = File.ReadAllText(path);

                    pluginVersionFromAssets = content;
                    break;
                }
            }

            return pluginVersionFromAssets;
        }


        private static void ReplaceUseOfFingersInPoseScriptableObjects()
        {
            HandPoseScriptableObject[] _handPoseSOs = Resources.FindObjectsOfTypeAll(typeof(HandPoseScriptableObject)) as HandPoseScriptableObject[];

            foreach (var _handPoseSO in _handPoseSOs)
            {
                var _path = AssetDatabase.GetAssetPath(_handPoseSO);
                var _text = File.ReadAllText(_path);
                _text = _text.Replace("Fingers", "fingers");
                File.WriteAllText(_path, _text);

                EditorUtility.SetDirty(_handPoseSO);
                AssetDatabase.Refresh();
            }

        }
#endif
    }
}