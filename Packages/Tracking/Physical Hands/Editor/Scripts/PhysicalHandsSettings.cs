using Leap.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public class PhysicalHandsSettings : ScriptableObject
{
    public struct RecommendedSetting
    {
        public string recommended, description;
        public SerializedProperty property;
        public bool ignored, impactsPerformance;
    }

    private static PhysicalHandsSettings _instance;
    public static PhysicalHandsSettings Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            else
                return _instance = FindSettingsSO();
        }
        set { _instance = value; }
    }

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
    private SerializedObject _physicsManager;
    private SerializedObject _timeManager;

    [HideInInspector, SerializeField]
    public Dictionary<string, RecommendedSetting> recommendedSettings = new Dictionary<string, RecommendedSetting>();

    [HideInInspector, SerializeField]
    public bool ignoreBurst = false;

    [HideInInspector, SerializeField]
    public bool showAppliedSettings = false;
    [HideInInspector, SerializeField]
    public bool showIgnoredSettings = false;

    [HideInInspector, SerializeField]
    public bool showPhysicalHandsSettingsOnStartup = true;

    public void ResetToDefaults()
    {
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
        _physicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(PHYSICS_SETTINGS_ASSET_PATH)[0]);
        _timeManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(TIME_SETTINGS_ASSET_PATH)[0]);
        recommendedSettings.Clear();
        recommendedSettings = new Dictionary<string, RecommendedSetting>
            {
                {
                    ID_SLEEP_THRESHOLD,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_SLEEP_THRESHOLD),
                        recommended = "0.001",
                        description = "Increases the realism of your physics objects e.g. allows objects to correctly rest"
                    }
                },
                {
                    ID_DEFAULT_MAX_ANGULAR_SPEED,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_DEFAULT_MAX_ANGULAR_SPEED),
                        recommended = "100",
                        description = "Allows you to rotate objects more closely to the hand tracking data"
                    }
                },
                {
                    ID_DEFAULT_CONTACT_OFFSET,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_DEFAULT_CONTACT_OFFSET),
                        recommended = "0.001",
                        description = "Distance used by physics sim to generate collision contacts. "
                    }
                },
                {
                    ID_AUTO_SYNC_TRANSFORMS,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_AUTO_SYNC_TRANSFORMS),
                        recommended = "False",
                        description = "Automatically update transform positions and rotations in the physics sim. If enabled, may cause jitter on rigidbodies when grabbed."
                    }
                },
                {
                    ID_CONTACTS_GENERATION,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_CONTACTS_GENERATION),
                        recommended = "Persistent Contact Manifold",
                        description = "Recommended default by unity for?generating contacts every physics frame."
                    }
                },
                {
                    ID_GRAVITY,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_GRAVITY),
                        recommended = new Vector3(0, -4.905f, 0).ToString(),
                        description = "Makes things easier to manipulate with hands in VR"
                    }
                },
                {
                    ID_MAX_DEPEN_VEL,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_MAX_DEPEN_VEL),
                        //TODO confirm this is not actually 2!
                        recommended = "1",
                        description = "Reduces unwanted physics explosions"
                    }
                },
                {
                    ID_FRICTION_TYPE,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_FRICTION_TYPE),
                        recommended = "Patch Friction Type",
                        description = "The most stable friction type unity provides"
                    }
                },
                {
                    ID_IMPROVED_PATCH_FRICTION,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_IMPROVED_PATCH_FRICTION),
                        recommended = "True",
                        description = "Guarantees static and dynamic friction do not exceed analytical results"
                    }
                },
                {
                    ID_BOUNCE_THRESHOLD,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_BOUNCE_THRESHOLD),
                        recommended = "2",
                        description = "Reduces unwanted bounces."
                    }
                },
                {
                    ID_SOLVER_ITERATIONS,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_SOLVER_ITERATIONS),
                        recommended = "15",
                        description = "Improves physics system stability."
                    }
                },
                {
                    ID_SOLVER_VEL_ITERATIONS,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_SOLVER_VEL_ITERATIONS),
                        recommended = "5",
                        description = "Improves physics system velocity stability after a bounce.",
                        impactsPerformance = true
                    }
                },
                {
                    ID_ENHANCED_DETERMINISM,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_ENHANCED_DETERMINISM),
                        recommended = "True",
                        description = "Reduce instability and randomness of the physics sim.",
                        impactsPerformance = true
                    }
                },
                {
                    ID_SOLVER_TYPE,
                    new RecommendedSetting()
                    {
                        property = _physicsManager.FindProperty(ID_SOLVER_TYPE),
                        recommended = "Temporal Gauss Seidel",
                        description = "2021 only. Ensures that rotations for Hard Contact Hands are calculated correctly.",
                        impactsPerformance = true
                    }
                },
                {
                    ID_FIXED_TIMESTEP,
                    new RecommendedSetting()
                    {
                        property = _timeManager.FindProperty(ID_FIXED_TIMESTEP),
                        recommended = "0.011111",
                        description = "Makes your app physics run smoother.",
                        impactsPerformance = true
                    }
                }
            };
    }

    public bool AllSettingsApplied()
    {
#if !BURST_AVAILABLE
        if (!ignoreBurst) return false;
#endif
        foreach (RecommendedSetting setting in recommendedSettings.Values)
        {
            if (!setting.ignored && !IsRecommendedSettingApplied(setting))
            {
                return false;
            }
        }
        return true;
    }

    public bool IsRecommendedSettingApplied(string key)
    {
        RecommendedSetting recommendedSetting = recommendedSettings[key];
        return IsRecommendedSettingApplied(recommendedSetting);
    }

    public bool IsRecommendedSettingApplied(RecommendedSetting recommendedSetting)
    {
        return recommendedSetting.property.ValueToString().ToLower() == recommendedSetting.recommended.ToLower();
    }

    public void ApplyAllRecommendedSettings()
    {
        foreach (var key in recommendedSettings.Keys)
        {
            if (!recommendedSettings[key].ignored)
            {
                ApplyRecommendedSetting(key);
            }
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
        _physicsManager.ApplyModifiedProperties();
        _timeManager.ApplyModifiedProperties();
    }

    #region Scriptable Object Setup
#if UNITY_EDITOR
    [MenuItem("Ultraleap/Open Physical Hands Settings")]
    private static void SelectULSettingsDropdown()
    {
        SettingsService.OpenProjectSettings("Project/Ultraleap/Physical Hands");
    }
#endif

    private static PhysicalHandsSettings FindSettingsSO()
    {
        // Try to directly load the asset
        PhysicalHandsSettings ultraleapSettings = Resources.Load<PhysicalHandsSettings>("Physical Hands Settings");

        if (ultraleapSettings != null)
        {
            _instance = ultraleapSettings;
            return _instance;
        }

        PhysicalHandsSettings[] settingsSO = Resources.FindObjectsOfTypeAll(typeof(PhysicalHandsSettings)) as PhysicalHandsSettings[];

        if (settingsSO != null && settingsSO.Length > 0)
        {
            _instance = settingsSO[0]; // Assume there is only one settings file
        }
        else
        {
            _instance = CreateSettingsSO();
        }

        return _instance;
    }

    static PhysicalHandsSettings CreateSettingsSO()
    {
        PhysicalHandsSettings newSO = null;
#if UNITY_EDITOR
        newSO = ScriptableObject.CreateInstance<PhysicalHandsSettings>();

        Directory.CreateDirectory(Application.dataPath + "/Resources/");
        AssetDatabase.CreateAsset(newSO, "Assets/Resources/Physical Hands Settings.asset");
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