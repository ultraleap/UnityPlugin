using Leap.Unity;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class ContactHandsSettingsProvider
{
    static MultiColumnHeaderState columnState;
    static MultiColumnHeader header;
    static ContactHandsTreeView treeView;

    private const int SMALL_SPACE = 5;
    private const int MEDIUM_SPACE = 10;
    private const int LARGE_SPACE = 20;

    private static string performanceList = "";

    private ContactHandsSettingsProvider()
    {

    }

    [SettingsProvider]
    public static SettingsProvider CreateContactHandsSettingsProvider()
    {
        // First parameter is the path in the Settings window.
        // Second parameter is the scope of this setting: it only appears in the Project Settings window.
        var provider = new SettingsProvider("Project/Ultraleap/Contact Hands", SettingsScope.Project)
        {
            // Populate the search keywords to enable smart search filtering and label highlighting:
            keywords = new HashSet<string>(new[] { "Number", "Sleep Threshold" })
        };

        provider.guiHandler += OnGUI;
        provider.activateHandler += OnActivate;
        EditorApplication.delayCall += FirstLoad;

        return provider;
    }

    private static void FirstLoad()
    {
        // Unload so we don't keep trying do it over and over
        EditorApplication.delayCall -= FirstLoad;
        if (!ContactHandsSettings.Instance.AllSettingsApplied())
        {
            SettingsService.OpenProjectSettings("Project/Ultraleap/Contact Hands");
        }
    }

    private static void OnActivate(string arg1, VisualElement element)
    {
        columnState = ContactHandsTreeView.CreateDefaultMultiColumnHeaderState();
        header = new MultiColumnHeader(columnState);
        treeView = new ContactHandsTreeView(new TreeViewState(), header);

        performanceList = "";
        foreach (ContactHandsSettings.RecommendedSetting setting in ContactHandsSettings.Instance.recommendedSettings.Values)
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
        ContactHandsSettings.Instance.showContactHandsSettingsOnStartup = GUILayout.Toggle(ContactHandsSettings.Instance.showContactHandsSettingsOnStartup,
            new GUIContent("Show Contact Hands settings on project start if unapplied settings"));
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
        bool newValue = GUILayout.Toggle(ContactHandsSettings.Instance.showAppliedSettings, new GUIContent("Show Applied Settings"));

        if (newValue != ContactHandsSettings.Instance.showAppliedSettings)
        {
            ContactHandsSettings.Instance.showAppliedSettings = newValue;
            treeView.Reload();
        }

        GUILayout.Space(30);

        newValue = GUILayout.Toggle(ContactHandsSettings.Instance.showIgnoredSettings, new GUIContent("Show Ignored Settings"));

        if (newValue != ContactHandsSettings.Instance.showIgnoredSettings)
        {
            ContactHandsSettings.Instance.showIgnoredSettings = newValue;
            treeView.Reload();
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Apply All", GUILayout.Width(75)))
        {
            ContactHandsSettings.Instance.ApplyAllRecommendedSettings();
            treeView.Reload();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndHorizontal();

        Rect rect = GUILayoutUtility.GetRect(800, 800, 0, 300, EditorStyles.selectionRect);
        rect.x += LARGE_SPACE;
        treeView.OnGUI(rect);

        if (!(!ContactHandsSettings.Instance.showIgnoredSettings && ContactHandsSettings.Instance.ignoreBurst))
        {
            BurstSetting();
        }

        EditorGUI.indentLevel--;
    }

    private static void BurstSetting()
    {
        if (!ContactHandsSettings.Instance.showAppliedSettings)
        {
#if BURST_AVAILABLE
            return;
#endif
        }
        else
        {
            GUI.enabled = !ContactHandsSettings.Instance.showAppliedSettings;
        }
        EditorGUILayout.BeginHorizontal(GUILayout.Width(820));
        GUILayout.Space(5);
        EditorGUILayout.HelpBox($"Please install the Unity Burst package, otherwise overall performance will be impacted.", MessageType.Warning);

        if (GUILayout.Button("Install Package", GUILayout.Width(100), GUILayout.Height(38)))
        {
            UnityEditor.PackageManager.Client.Add("com.unity.burst");
        }

        GUI.enabled = true;
        if (ContactHandsSettings.Instance.ignoreBurst)
        {
            if (GUILayout.Button("Watch", GUILayout.Width(100), GUILayout.Height(38)))
            {
                ContactHandsSettings.Instance.ignoreBurst = false;
            }
        }
        else
        {
            if (GUILayout.Button("Ignore", GUILayout.Width(100), GUILayout.Height(38)))
            {
                ContactHandsSettings.Instance.ignoreBurst = true;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

}
