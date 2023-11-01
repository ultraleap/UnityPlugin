using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public class PhysicalHandsSettingsProvider
{
    static MultiColumnHeaderState columnState;
    static MultiColumnHeader header;
    static PhysicalHandsTreeView treeView;

    private const int SMALL_SPACE = 5;
    private const int MEDIUM_SPACE = 10;
    private const int LARGE_SPACE = 20;

    private static string performanceList = "";

    private PhysicalHandsSettingsProvider()
    {
        EditorApplication.delayCall += FirstLoad;
    }

    [SettingsProvider]
    public static SettingsProvider CreatePhysicalHandsSettingsProvider()
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

    private static void FirstLoad()
    {
        // Unload so we don't keep trying do it over and over
        EditorApplication.delayCall -= FirstLoad;

        if (PhysicalHandsSettings.Instance.showPhysicalHandsSettingsOnStartup && !PhysicalHandsSettings.Instance.AllSettingsApplied())
        {
            SettingsService.OpenProjectSettings("Project/Ultraleap/Physical Hands");
        }
    }

    private static void OnActivate(string arg1, VisualElement element)
    {
        columnState = PhysicalHandsTreeView.CreateDefaultMultiColumnHeaderState();
        header = new MultiColumnHeader(columnState);
        treeView = new PhysicalHandsTreeView(new TreeViewState(), header);

        performanceList = "";
        foreach (PhysicalHandsSettings.RecommendedSetting setting in PhysicalHandsSettings.Instance.recommendedSettings.Values)
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

        GUILayout.Space(SMALL_SPACE);
        EditorGUILayout.LabelField("Settings Config", EditorStyles.boldLabel);
        GUILayout.Space(SMALL_SPACE);

        GUILayout.BeginHorizontal();

        GUILayout.Space(LARGE_SPACE);
        PhysicalHandsSettings.Instance.showPhysicalHandsSettingsOnStartup = GUILayout.Toggle(PhysicalHandsSettings.Instance.showPhysicalHandsSettingsOnStartup,
            new GUIContent("Show Physical Hands settings on project start if unapplied settings"));
        GUILayout.EndHorizontal();

        GUILayout.Space(LARGE_SPACE);

        EditorGUILayout.LabelField("Recommended Settings", EditorStyles.boldLabel);
        GUILayout.Space(SMALL_SPACE);

        GUILayout.BeginHorizontal(GUILayout.Width(815));
        EditorGUILayout.HelpBox($"To ensure the best experience for your users, Ultraleap has provided recommended settings below.", MessageType.Info);
        GUILayout.EndHorizontal();

        GUILayout.Space(SMALL_SPACE);

        GUILayout.BeginHorizontal(GUILayout.Width(815));
        EditorGUILayout.HelpBox($"The following recommended settings impact performance:" + performanceList, MessageType.Info);
        GUILayout.EndHorizontal();

        GUILayout.Space(LARGE_SPACE);

        GUILayout.BeginHorizontal(GUILayout.Width(820));
        GUILayout.Space(LARGE_SPACE);

        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(SMALL_SPACE);
        bool newValue = GUILayout.Toggle(PhysicalHandsSettings.Instance.showAppliedSettings, new GUIContent("Show Applied Settings"));

        if (newValue != PhysicalHandsSettings.Instance.showAppliedSettings)
        {
            PhysicalHandsSettings.Instance.showAppliedSettings = newValue;
            treeView.Reload();
        }

        GUILayout.Space(30);

        newValue = GUILayout.Toggle(PhysicalHandsSettings.Instance.showIgnoredSettings, new GUIContent("Show Ignored Settings"));

        if (newValue != PhysicalHandsSettings.Instance.showIgnoredSettings)
        {
            PhysicalHandsSettings.Instance.showIgnoredSettings = newValue;
            treeView.Reload();
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Apply All", GUILayout.Width(75)))
        {
            PhysicalHandsSettings.Instance.ApplyAllRecommendedSettings();
            InstallBurst();
            treeView.Reload();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndHorizontal();

        Rect rect = GUILayoutUtility.GetRect(800, 800, 0, 300, EditorStyles.selectionRect);
        rect.x += LARGE_SPACE;
        treeView.OnGUI(rect);

        if (!(!PhysicalHandsSettings.Instance.showIgnoredSettings && PhysicalHandsSettings.Instance.ignoreBurst))
        {
            BurstSetting();
        }

        EditorGUI.indentLevel--;
    }

    private static void BurstSetting()
    {
        if (!PhysicalHandsSettings.Instance.showAppliedSettings)
        {
#if BURST_AVAILABLE
            return;
#endif
        }
        else
        {
            GUI.enabled = !PhysicalHandsSettings.Instance.showAppliedSettings;
        }
        EditorGUILayout.BeginHorizontal(GUILayout.Width(820));
        GUILayout.Space(5);
        EditorGUILayout.HelpBox($"Please install the Unity Burst package, otherwise overall performance will be impacted.", MessageType.Warning);

        if (GUILayout.Button("Install Package", GUILayout.Width(100), GUILayout.Height(38)))
        {
            InstallBurst();
        }

        GUI.enabled = true;
        if (PhysicalHandsSettings.Instance.ignoreBurst)
        {
            if (GUILayout.Button("Watch", GUILayout.Width(100), GUILayout.Height(38)))
            {
                PhysicalHandsSettings.Instance.ignoreBurst = false;
            }
        }
        else
        {
            if (GUILayout.Button("Ignore", GUILayout.Width(100), GUILayout.Height(38)))
            {
                PhysicalHandsSettings.Instance.ignoreBurst = true;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void InstallBurst()
    {
#if BURST_AVAILABLE
        return;
#endif
        if (PhysicalHandsSettings.Instance.ignoreBurst) { return; }

        UnityEditor.PackageManager.Client.Add("com.unity.burst");
    }
}