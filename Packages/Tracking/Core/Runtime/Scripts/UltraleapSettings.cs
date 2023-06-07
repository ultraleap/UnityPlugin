
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
            EditorGUILayout.LabelField("The Leap subsystem should be used when you do not want to use OpenXR for hand tracking inputs.", readOnlyText);
            
            var settingsTarget = target as UltraleapSettings;
            bool editorBool = settingsTarget.LeapSubsystemEnabled;
            settingsTarget.LeapSubsystemEnabled = EditorGUILayout.Toggle("enable the leap subsystem ", editorBool);
            EditorGUILayout.EndVertical();
        }
    }


    [CreateAssetMenu(menuName = "ScriptableObjects/Settings/PluginSettings")]
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