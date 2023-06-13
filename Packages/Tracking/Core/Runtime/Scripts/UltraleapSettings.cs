using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
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
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Leap XRHands Subsystem", EditorStyles.boldLabel);
            
            var readOnlyText = new GUIStyle(EditorStyles.textField);
            readOnlyText.wordWrap = true;
            EditorGUILayout.HelpBox("The Leap XRHands Subsystem passes hand input directly from the Leap Service to XRHands. " +
                "If you are using OpenXR for hand input, do not enable this option. " +
                "Instead, use the Hand Tracking Subsystem option in the OpenXR XR Plug-in Management settings." +
                "\r\n\nThis option can not be toggled at runtime.", MessageType.Info, true); ;
            
            var settingsTarget = target as UltraleapSettings;
            bool editorBool = settingsTarget.LeapSubsystemEnabled;
            settingsTarget.LeapSubsystemEnabled = EditorGUILayout.ToggleLeft("Enable the leap subsystem ", editorBool);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(30);
            if(GUILayout.Button("Reset To Defaults"))
            {
                if(EditorUtility.DisplayDialog("Reset all settings", "This will reset all settings in this Ultraleap settings file", "Yes", "No"))
                {
                    settingsTarget.ResetToDefaults();
                }
            }
        }
    }
#endif

    public class UltraleapSettings : ScriptableObject
    {
        public static event Action enableUltraleapSubsystem;
        public static event Action disableUltraleapSubsystem;

        private bool _leapSubsystemEnabled;

        [SerializeField]
        public bool LeapSubsystemEnabled
        {
            get { return _leapSubsystemEnabled; }
            set
            {
                if (value != _leapSubsystemEnabled)
                {
                    _leapSubsystemEnabled = value;
                    if (_leapSubsystemEnabled == false)
                    {
                        disableUltraleapSubsystem?.Invoke();
                    }
                    else
                    {
                        enableUltraleapSubsystem?.Invoke();
                    }
                }
            }
        }

        public void ResetToDefaults()
        {
            LeapSubsystemEnabled = false;
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
            if (settingsSO[0] != null)
            {
                return settingsSO[0]; // Assume there is only one settings file
            }
            else
            {
                return null;
            }
        }
    }
}