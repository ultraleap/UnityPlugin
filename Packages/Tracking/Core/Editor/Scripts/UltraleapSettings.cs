using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
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

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("If using OpenXR for hand input, use the Hand Tracking Subsystem in XR Plug-in Management/OpenXR. Do not enable both subsystems." +
                "\r\n\nThis option can not be toggled at runtime.", MessageType.Info, true);

            EditorGUILayout.Space(5);

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty leapSubsystemEnabledProperty = settings.FindProperty("leapSubsystemEnabled");
                leapSubsystemEnabledProperty.boolValue = EditorGUILayout.ToggleLeft("Enable Leap XRHands Subsystem", leapSubsystemEnabledProperty.boolValue);
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty updateLeapInputSystemProperty = settings.FindProperty("updateLeapInputSystem");
                updateLeapInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Leap Input System", updateLeapInputSystemProperty.boolValue);

                SerializedProperty updateMetaInputSystemProperty = settings.FindProperty("updateMetaInputSystem");
                updateMetaInputSystemProperty.boolValue = EditorGUILayout.ToggleLeft("Update Meta Aim Input System", updateMetaInputSystemProperty.boolValue);
            }
            EditorGUILayout.Space(30);

            if (GUILayout.Button("Reset To Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset all settings", "This will reset all settings in this Ultraleap settings file", "Yes", "No"))
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

        private SerializedObject physicsManager;
        private SerializedObject timeManager;
        private const string PHYSICS_SETTINGS_ASSET_PATH = "ProjectSettings/DynamicsManager.asset";
        private const string TIME_SETTINGS_ASSET_PATH = "ProjectSettings/TimeManager.asset";
        private const string ID_SLEEP_THRESHOLD = "m_SleepThreshold";
        private const string ID_DEFAULT_MAX_ANGULAR_SPEED = "m_DefaultMaxAngularSpeed";
        private const string ID_DEFAULT_CONTACT_OFFSET = "m_DefaultContactOffset";
        private const string ID_AUTO_SYNC_TRANSFORMS = "m_AutoSyncTransforms";
        private const string ID_CONTACTS_GENERATION = "m_ContactsGeneration";
        private const string ID_GRAVITY = "m_Gravity";
        private const string ID_MAX_DEPEN_VEL = "m_DefaultMaxDepenetrationVelocity";
        private const string ID_FRICTION_TYPE = "m_FrictionType";
        private const string ID_IMPROVED_PATCH_FRICTION = "m_ImprovedPatchFriction";
        private const string ID_BOUNCE_THRESHOLD = "m_BounceThreshold";
        private const string ID_SOLVER_ITERATIONS = "m_DefaultSolverIterations";
        private const string ID_SOLVER_VEL_ITERATIONS = "m_DefaultSolverVelocityIterations";
        private const string ID_ENHANCED_DETERMINISM = "m_EnableEnhancedDeterminism";
        private const string ID_SOLVER_TYPE = "m_SolverType";
        private const string ID_FIXED_TIMESTEP = "Fixed Timestep";

        public struct RecommendedSetting
        {
            public string recommended, description;
            public SerializedProperty property;
            public bool ignored;
        }

        [HideInInspector, SerializeField]
        public Dictionary<string, RecommendedSetting> recommendedSettings = new Dictionary<string, RecommendedSetting>();

        [HideInInspector, SerializeField]
        public bool ignoreBurst = false;

        [HideInInspector, SerializeField]
        public bool showAppliedSettings = false;
        [HideInInspector, SerializeField]
        public bool showIgnoredSettings = false;

        [HideInInspector, SerializeField]
        public bool showUltraleapSettingsOnStartup = false;

        public void ResetToDefaults()
        {
            leapSubsystemEnabled = false;
            updateLeapInputSystem = false;
            updateMetaInputSystem = false;
            RefreshRecommendedSettingsValues();
            ResetRecommendedSettingsStates();
        }

        public void ResetRecommendedSettingsStates()
        {
            foreach (var recommendedSetting in recommendedSettings)
            {
                RecommendedSetting modifiedSetting = recommendedSetting.Value;
                modifiedSetting.ignored = false;
                recommendedSettings[recommendedSetting.Key] = modifiedSetting;
            }
        }
        public void RefreshRecommendedSettingsValues()
        {
            physicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(PHYSICS_SETTINGS_ASSET_PATH)[0]);
            timeManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(TIME_SETTINGS_ASSET_PATH)[0]);
            recommendedSettings.Clear();
            recommendedSettings = new Dictionary<string, RecommendedSetting>
            {
                {
                    ID_SLEEP_THRESHOLD,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_SLEEP_THRESHOLD),
                        recommended = "0.001",
                        description = "Increases the realism of your physics objects e.g. allows objects to correctly rest"
                    }
                },
                {
                    ID_DEFAULT_MAX_ANGULAR_SPEED,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_DEFAULT_MAX_ANGULAR_SPEED),
                        recommended = "100",
                        description = "Allows you to rotate objects more closely to the hand tracking data"
                    }
                },
                {
                    ID_DEFAULT_CONTACT_OFFSET,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_DEFAULT_CONTACT_OFFSET),
                        recommended = "0.001",
                        description = "Distance used by physics sim to generate collision contacts. "
                    }
                },
                {
                    ID_AUTO_SYNC_TRANSFORMS,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_AUTO_SYNC_TRANSFORMS),
                        recommended = "False",
                        description = "Automatically update transform positions and rotations in the physics sim. If enabled, may cause jitter on rigidbodies when grabbed."
                    }
                },
                {
                    ID_CONTACTS_GENERATION,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_CONTACTS_GENERATION),
                        recommended = "Persistent Contact Manifold",
                        description = "Recommended default by unity for generating contacts every physics frame."
                    }
                },
                {
                    ID_GRAVITY,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_GRAVITY),
                        recommended = new Vector3(0, -4.905f, 0).ToString(),
                        description = "Makes things easier to manipulate with hands in VR"
                    }
                },
                {
                    ID_MAX_DEPEN_VEL,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_MAX_DEPEN_VEL),
                        //TODO confirm this is not actually 2!
                        recommended = "1",
                        description = "Reduces unwanted physics explosions"
                    }
                },
                {
                    ID_FRICTION_TYPE,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_FRICTION_TYPE),
                        recommended = "Patch Friction Type",
                        description = "The most stable friction type unity provides"
                    }
                },
                {
                    ID_IMPROVED_PATCH_FRICTION,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_IMPROVED_PATCH_FRICTION),
                        recommended = "True",
                        description = "Guarantees static and dynamic friction do not exceed analytical results"
                    }
                },
                {
                    ID_BOUNCE_THRESHOLD,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_BOUNCE_THRESHOLD),
                        recommended = "2",
                        description = "Reduces unwanted bounces."
                    }
                },
                {
                    ID_SOLVER_ITERATIONS,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_SOLVER_ITERATIONS),
                        recommended = "15",
                        description = "Improves physics system stability."
                    }
                },
                {
                    ID_SOLVER_VEL_ITERATIONS,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_SOLVER_VEL_ITERATIONS),
                        recommended = "5",
                        description = "Improves physics system velocity stability after a bounce."
                    }
                },
                {
                    ID_ENHANCED_DETERMINISM,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_ENHANCED_DETERMINISM),
                        recommended = "True",
                        description = "Reduce instability and randomness of the physics sim."
                    }
                },
                {
                    ID_SOLVER_TYPE,
                    new RecommendedSetting()
                    {
                        property = physicsManager.FindProperty(ID_SOLVER_TYPE),
                        recommended = "Temporal Gauss Seidel",
                        description = "2021 only. Ensures that rotations for Hard Contact Hands are calculated correctly."
                    }
                },
                {
                    ID_FIXED_TIMESTEP,
                    new RecommendedSetting()
                    {
                        property = timeManager.FindProperty(ID_FIXED_TIMESTEP),
                        recommended = "0.011111",
                        description = "Makes your app physics run smoother."
                    }
                }
            };
        }

        public bool IsRecommendedSettingApplied(string key)
        {
            RecommendedSetting recommendedSetting = recommendedSettings[key];
            return recommendedSetting.property.ValueToString().ToLower() == recommendedSetting.recommended.ToLower();
        }

        public void ApplyAllRecommendedSettings()
        {
            foreach (var key in recommendedSettings.Keys)
            {
                ApplyRecommendedSetting(key);
            }
        }

        public void ApplyRecommendedSetting(string key)
        {
            RecommendedSetting recommendedSetting = recommendedSettings[key];

            SerializedProperty property = recommendedSetting.property;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    property.boolValue = Convert.ToBoolean(recommendedSetting.recommended.ToLower());
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = float.Parse(recommendedSetting.recommended);
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = int.Parse(recommendedSetting.recommended);
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = property.enumDisplayNames.ToList().IndexOf(recommendedSetting.recommended);
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = recommendedSetting.recommended.ToVector3();
                    break;
            }
            physicsManager.ApplyModifiedProperties();
            timeManager.ApplyModifiedProperties();
        }

        #region Scriptable Object Setup
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

#if UNITY_EDITOR
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(FindSettingsSO());
        }
#endif
        #endregion
    }

}