// Register a SettingsProvider using UIElements for the drawing framework:
using Leap.Unity;
using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

public class ContactHandsSettings
{
    [SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider()
    {
        // First parameter is the path in the Settings window.
        // Second parameter is the scope of this setting: it only appears in the Project Settings window.
        var provider = new SettingsProvider("Project/Ultraleap/Custom", SettingsScope.Project)
        {
            // By default the last token of the path is used as display name if no label is provided.
            label = "Contact Hands Settings",

            // Populate the search keywords to enable smart search filtering and label highlighting:
            keywords = new HashSet<string>(new[] { "Number", "Sleep Threshold" })
        };

        provider.guiHandler += OnGUI;
        provider.activateHandler += OnActivate;

        return provider;
    }

    private static void OnActivate(string arg1, VisualElement element)
    {
        columnState = ContactHandsTreeView.CreateDefaultMultiColumnHeaderState();
        header = new MultiColumnHeader(columnState);
        treeView = new ContactHandsTreeView(new TreeViewState(), header);
    }


    static MultiColumnHeaderState columnState;
    static MultiColumnHeader header;
    static ContactHandsTreeView treeView;

    private static void OnGUI(string searchContext)
    {
        GUILayout.Space(20);
        UltraleapSettings.Instance.showUltraleapSettingsOnStartup = GUILayout.Toggle(UltraleapSettings.Instance.showUltraleapSettingsOnStartup, new GUIContent("Show Contact Hands settings on project start if unapplied settings"));

        GUILayout.Space(20);

        EditorGUILayout.LabelField("Apply all recommended settings", EditorStyles.boldLabel);
        GUILayout.Space(pixels: 10);

        GUILayout.BeginHorizontal(GUILayout.Width(810));
        GUILayout.Space(10);

        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(10);
        bool newValue = GUILayout.Toggle(UltraleapSettings.Instance.showAppliedSettings, new GUIContent("Show Applied Settings"));

        if (newValue != UltraleapSettings.Instance.showAppliedSettings) {
            UltraleapSettings.Instance.showAppliedSettings = newValue;
            treeView.Reload();
        }

        GUILayout.Space(30);
        
        newValue = GUILayout.Toggle(UltraleapSettings.Instance.showIgnoredSettings, new GUIContent("Show Ignored Settings"));

        if (newValue != UltraleapSettings.Instance.showIgnoredSettings)
        {
            UltraleapSettings.Instance.showIgnoredSettings = newValue;
            treeView.Reload();
        }



        GUILayout.Space(100);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Apply All", GUILayout.Width(75)))
        {
            UltraleapSettings.Instance.ApplyAllRecommendedSettings();
            treeView.Reload();
        }

        GUILayout.EndHorizontal();
        
        GUILayout.EndHorizontal();

        Rect rect = GUILayoutUtility.GetRect(800, 800, 0, 300, EditorStyles.selectionRect);
        rect.x += 10;
        treeView.OnGUI(rect);

    }


}
