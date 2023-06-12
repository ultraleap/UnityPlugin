
using System;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

namespace Leap.Unity
{

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
            EditorGUILayout.HelpBox("The Leap subsystem should be used when not using OpenXR for hand tracking inputs." +
                "\n \n This cannot be toggled during runtime.", MessageType.Info, true); ;
            
            var settingsTarget = target as UltraleapSettings;
            bool editorBool = settingsTarget.LeapSubsystemEnabled;
            EditorGUILayout.BeginHorizontal();
            settingsTarget.LeapSubsystemEnabled = EditorGUILayout.ToggleLeft("Enable the leap subsystem ", editorBool);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }


    //[CreateAssetMenu(menuName = "Ultraleap/Settings/PluginSettings")]  /// comment out so the user cannot create these but may be needed for development
    public class UltraleapSettings : ScriptableObject
    {
        public static event Action enableUltraleapSubsystem;
        public static event Action disableUltraleapSubsystem;

        private bool _leapSubsystemEnabled = false;

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
    }
}