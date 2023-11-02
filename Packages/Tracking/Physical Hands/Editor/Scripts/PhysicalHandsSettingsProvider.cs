using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Leap.Unity.PhysicalHands
{
    [InitializeOnLoad]
    internal class PhysicalHandsSettingsProvider
    {
        private const int SMALL_SPACE = 5;
        private const int LARGE_SPACE = 20;

        private static string performanceList = "";

        [SettingsProvider]
        internal static SettingsProvider CreatePhysicalHandsSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/Ultraleap/Physical Hands", SettingsScope.Project)
            {
                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Number", "Sleep Threshold" })
            };

            provider.guiHandler += OnGUI;
            provider.activateHandler += OnActivate;

            return provider;
        }

        private static void OnActivate(string arg1, VisualElement element)
        {
            PhysicalHandsSettings.RefreshRecommendedSettingsValues();
            performanceList = "";
            foreach (PhysicalHandsSettings.RecommendedSetting setting in PhysicalHandsSettings.recommendedSettings.Values)
            {
                if (setting.impactsPerformance)
                {
                    performanceList += "\n- " + setting.property.displayName;
                }
            }
        }

        private static void OnGUI(string searchContext)
        {
            EditorGUI.indentLevel++;

            GUILayout.Space(LARGE_SPACE);
            EditorGUILayout.LabelField("Recommended Settings", EditorStyles.boldLabel);
            GUILayout.Space(SMALL_SPACE);

            if (PhysicalHandsSettings.AllSettingsApplied())
            {
                EditorGUILayout.LabelField("All recommended settings have been applied");
            }
            else
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"To ensure the best experience for your users, Ultraleap has provided recommended settings below.", MessageType.Info);
                GUILayout.EndHorizontal();

                GUILayout.Space(SMALL_SPACE);

                GUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"The following recommended settings impact performance:" + performanceList, MessageType.Info);
                GUILayout.EndHorizontal();

                GUILayout.Space(LARGE_SPACE);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Apply All", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    PhysicalHandsSettings.ApplyAllRecommendedSettings();
                }
                GUILayout.Space(SMALL_SPACE);
                GUILayout.EndHorizontal();

                DrawRecommended();
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawRecommended()
        {
            foreach (var recommended in PhysicalHandsSettings.recommendedSettings)
            {
                bool settingApplied = PhysicalHandsSettings.IsRecommendedSettingApplied(recommended.Key);

                if (!settingApplied)
                {
                    EditorGUILayout.LabelField(new GUIContent(recommended.Value.property.displayName, recommended.Value.description), EditorStyles.boldLabel);

                    GUILayout.Space(SMALL_SPACE);

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(SMALL_SPACE);

                    GUIStyle myStyle = GUI.skin.GetStyle("HelpBox");
                    myStyle.richText = true;
                    myStyle.fontSize = 12;

                    EditorGUILayout.TextArea("Current: " + recommended.Value.property.ValueToString() + "\n" + "Recommended: " + recommended.Value.recommended, myStyle);

                    if (GUILayout.Button("Apply", GUILayout.Width(75), GUILayout.Height(30)))
                    {
                        PhysicalHandsSettings.ApplyRecommendedSetting(recommended.Key);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}