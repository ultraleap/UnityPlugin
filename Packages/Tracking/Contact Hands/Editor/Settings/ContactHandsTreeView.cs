using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

public class ContactHandsTreeView : TreeView
{
    const float ROW_HEIGHT = 30f;
    const float BUTTON_WIDTH = 60f;
    const float BUTTON_HEIGHT = 20f;
    const float BUTTON_PADDING = 2.5f;
    const string PROJECT_SETTINGS_ASSET_PATH = "ProjectSettings/DynamicsManager.asset";

    SerializedObject physicsManager;

    const string ID_SLEEP_THRESHOLD = "m_SleepThreshold";
    const string ID_DEFAULT_MAX_ANGULAR_SPEED = "m_DefaultMaxAngularSpeed";
    const string ID_DEFAULT_CONTACT_OFFSET = "m_DefaultContactOffset";
    const string ID_AUTO_SYNC_TRANSFORMS = "m_AutoSyncTransforms";
    const string ID_CONTACTS_GENERATION = "m_ContactsGeneration";

    private Dictionary<string, RecommendedSetting> recommendedSettings = new Dictionary<string, RecommendedSetting>();
    private struct RecommendedSetting
    {
        public string after, description;
        public SerializedProperty property;
        public bool ignored;
    }

    private enum Columns
    {
        Name,
        CurrentValue,
        RecommendedValue,
        Apply
    }

    public ContactHandsTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
    {
        rowHeight = ROW_HEIGHT;
        columnIndexForTreeFoldouts = 2;
        showAlternatingRowBackgrounds = true;
        showBorder = true;
        customFoldoutYOffset = (ROW_HEIGHT - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI

        Reload();
    }

    private void UpdateRecommendedSettingsStates()
    {
        physicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(PROJECT_SETTINGS_ASSET_PATH)[0]);
        recommendedSettings.Clear();
        recommendedSettings = new Dictionary<string, RecommendedSetting>
        {
            {
                ID_SLEEP_THRESHOLD,
                new RecommendedSetting()
                {
                    property = physicsManager.FindProperty(ID_SLEEP_THRESHOLD),
                    after = "0.001",
                    description = "Increases the realism of your physics objects e.g. allows objects to correctly rest",
                    ignored = false
                }
            },
            {
                ID_DEFAULT_MAX_ANGULAR_SPEED,
                new RecommendedSetting()
                {
                    property = physicsManager.FindProperty(ID_DEFAULT_MAX_ANGULAR_SPEED),
                    after = "100",
                    description = "Allows you to rotate objects more closely to the hand tracking data",
                    ignored = false
                }
            },
            {
                ID_DEFAULT_CONTACT_OFFSET,
                new RecommendedSetting()
                {
                    property = physicsManager.FindProperty(ID_DEFAULT_CONTACT_OFFSET),
                    after = "0.001",
                    description = "Distance used by physics sim to generate collision contacts. ",
                    ignored = false
                }
            },
            {
                ID_AUTO_SYNC_TRANSFORMS,
                new RecommendedSetting()
                {
                    property = physicsManager.FindProperty(ID_AUTO_SYNC_TRANSFORMS),
                    after = "False",
                    description = "Automatically update transform positions and rotations in the physics sim. If enabled, may cause jitter on rigidbodies when grabbed.",
                    ignored = false
                }
            },
            {
                ID_CONTACTS_GENERATION,
                new RecommendedSetting()
                {
                    property = physicsManager.FindProperty(ID_CONTACTS_GENERATION),
                    after = "Persistent Contact Manifold",
                    description = "Recommended default by unity for generating contacts every physics frame..",
                    ignored = true
                }
            },
        };
    }

    protected override TreeViewItem BuildRoot()
    {
        // BuildRoot is called every time Reload is called to ensure that TreeViewItems 
        // are created from data. Here we create a fixed set of items. In a real world example,
        // a data model should be passed into the TreeView and the items created from the model.

        // This section illustrates that IDs should be unique. The root item is required to 
        // have a depth of -1, and the rest of the items increment from that.
        var root = new TreeViewItem()
        {
            id = 0,
            depth = -1,
            displayName = "root"
        };

        UpdateRecommendedSettingsStates();

        List<TreeViewItem> myList = new List<TreeViewItem>();
        string[] ids = recommendedSettings.Keys.OrderBy(id => id).ToArray();

        for (int i = 0; i < ids.Length; i++)
        {
            myList.Add(new TreeViewItem(i, 0, ids[i]));
        }

        // Utility method that initializes the TreeViewItem.children and .parent for all items.
        SetupParentsAndChildrenFromDepths(root, myList);

        // Return root of the tree
        return root;
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        var item = (TreeViewItem)args.item;
        for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
        {
            DrawCellGUI(args.GetCellRect(i), item, (Columns)args.GetColumn(i), ref args);
        }
    }

    public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
    {
        var columns = new[]
        {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 200,
                    minWidth = (BUTTON_WIDTH *2) + BUTTON_PADDING * 4,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Current Value", "The current value of the property."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = (BUTTON_WIDTH *2) + BUTTON_PADDING * 4,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Recommended Value", "Ultraleap's recommended value."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = (BUTTON_WIDTH *2) + BUTTON_PADDING * 4,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Apply", "Apply our recommended property."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = BUTTON_WIDTH *3,
                    minWidth = (BUTTON_WIDTH *2) + BUTTON_PADDING * 4,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false,
                }
            };

        Assert.AreEqual(columns.Length, Enum.GetValues(typeof(Columns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

        var state = new MultiColumnHeaderState(columns);
        return state;
    }

    protected override bool CanMultiSelect(TreeViewItem item)
    {
        return false;
    }

    protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
    {
        //TODO include other vals in here
        return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void DrawCellGUI(Rect cellRect, TreeViewItem item, Columns column, ref RowGUIArgs args)
    {
        // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
        CenterRectUsingLineHeight(ref cellRect);

        RecommendedSetting recommendedSetting;
        if (!recommendedSettings.TryGetValue(item.displayName, out recommendedSetting)) return;

        GUIStyle label = new GUIStyle("TV Line");
        label.alignment = TextAnchor.MiddleRight;
        label.padding.left = 2;
        label.padding.right = 2;
        string contents = "";

        GUI.enabled = !recommendedSetting.ignored && recommendedSetting.property.ValueToString().ToLower() != recommendedSetting.after.ToLower();



        switch (column)
        {
            case Columns.Name:

                label.font = EditorStyles.boldLabel.font;
                label.fontStyle = EditorStyles.boldLabel.fontStyle;
                label.alignment = TextAnchor.MiddleLeft;

                contents = recommendedSetting.property.displayName;
                break;

            case Columns.CurrentValue:
                contents = recommendedSetting.property.ValueToString();
                break;

            case Columns.RecommendedValue:
                contents = recommendedSetting.after;
                break;
            case Columns.Apply:
                GUI.enabled = true;
                if (GUI.Button(new Rect(cellRect.xMax - ((BUTTON_WIDTH / 2) * 4) - ( BUTTON_PADDING * 2), cellRect.y + (BUTTON_HEIGHT / 4), BUTTON_WIDTH, BUTTON_HEIGHT), "Apply"))
                {
                    ApplyRecommendedSetting(recommendedSetting);
                    Reload();
                }

                if (recommendedSetting.ignored)
                {
                    if (GUI.Button(new Rect(cellRect.xMax - ((BUTTON_WIDTH / 2) * 2) - BUTTON_PADDING, cellRect.y + (BUTTON_HEIGHT / 4), BUTTON_WIDTH, BUTTON_HEIGHT), "Watch"))
                    {
                        recommendedSetting.ignored = false;
                        Reload();
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(cellRect.xMax - ((BUTTON_WIDTH / 2) * 2) - BUTTON_PADDING, cellRect.y + (BUTTON_HEIGHT / 4), BUTTON_WIDTH, BUTTON_HEIGHT), "Ignore"))
                    {
                        recommendedSetting.ignored = true;
                        Reload();
                    }
                }

                GUI.enabled = !recommendedSetting.ignored;

                break;
        }


        if(column != Columns.Apply)
        {
            GUI.Label(cellRect, new GUIContent(contents, recommendedSetting.description), label);
        }
        GUI.enabled = true;

    }

    public void ApplyAllRecommendedSettings()
    {
        foreach(var setting in recommendedSettings)
        {
            ApplyRecommendedSetting(setting.Value);
        }
    }

    private void ApplyRecommendedSetting(RecommendedSetting recommendedSetting)
    {
        SerializedProperty property = recommendedSetting.property;
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                property.boolValue = Convert.ToBoolean(recommendedSetting.after.ToLower());
                break;
            case SerializedPropertyType.Float:
                property.floatValue = float.Parse(recommendedSetting.after);
                break;
            case SerializedPropertyType.Integer:
                property.intValue = int.Parse(recommendedSetting.after);
                break;
            case SerializedPropertyType.Enum:
                property.enumValueIndex = property.enumDisplayNames.ToList().IndexOf(recommendedSetting.after);
                   break;
        }
        physicsManager.ApplyModifiedProperties();
    }

    protected void CenterRectUsingLineHeight(ref Rect rect)
    {
        if (rect.height > ROW_HEIGHT)
        {
            rect.y += (rect.height - ROW_HEIGHT) * 0.5f;
            rect.height = ROW_HEIGHT;
        }
    }
}
