using Leap.Unity;
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

    private enum Columns
    {
        Name,
        CurrentValue,
        RecommendedValue,
        Apply,
        Ignore
    }

    public ContactHandsTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
    {
        UltraleapSettings.Instance.RefreshRecommendedSettingsValues();

        rowHeight = ROW_HEIGHT;
        columnIndexForTreeFoldouts = 2;
        showAlternatingRowBackgrounds = true;
        showBorder = true;
        customFoldoutYOffset = (ROW_HEIGHT - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI

        Reload();
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

        List<TreeViewItem> myList = new List<TreeViewItem>();

        var recommendedSettings = UltraleapSettings.Instance.recommendedSettings;

        string[] ids = recommendedSettings.Keys.OrderBy(id => UltraleapSettings.Instance.IsRecommendedSettingApplied(id)).ThenBy(id => recommendedSettings[id].ignored).ThenBy(id => recommendedSettings[id].property.displayName).ToArray();

        for (int i = 0; i < ids.Length; i++)
        {
            UltraleapSettings.RecommendedSetting recommendedSetting;
            if (!recommendedSettings.TryGetValue(ids[i], out recommendedSetting)) continue;

            bool settingApplied = UltraleapSettings.Instance.IsRecommendedSettingApplied(ids[i]);

            if (settingApplied && !UltraleapSettings.Instance.showAppliedSettings) continue;
            if (recommendedSetting.ignored && !UltraleapSettings.Instance.showIgnoredSettings) continue;

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
                    width = 220,
                    minWidth = 220,
                    maxWidth = 220,
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
                    width = 170,
                    minWidth = 170,
                    maxWidth = 170,
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
                    width = 170,
                    minWidth = 170,
                    maxWidth = 170,
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
                    width = BUTTON_WIDTH *1.5f,
                    minWidth = BUTTON_WIDTH *1.5f,
                    maxWidth = BUTTON_WIDTH *1.5f,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Ignore", "Ignore this property."),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = BUTTON_WIDTH *1.5f,
                    minWidth = BUTTON_WIDTH *1.5f,
                    maxWidth = BUTTON_WIDTH *1.5f,
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
        string recommendedSettingKey = item.displayName;
        UltraleapSettings.RecommendedSetting recommendedSetting;
        if (!UltraleapSettings.Instance.recommendedSettings.TryGetValue(recommendedSettingKey, out recommendedSetting)) return;
        bool settingApplied = UltraleapSettings.Instance.IsRecommendedSettingApplied(recommendedSettingKey);

        // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
        CenterRectUsingLineHeight(ref cellRect);

        GUIStyle label = new GUIStyle("TV Line");
        label.alignment = TextAnchor.MiddleRight;
        label.padding.left = 2;
        label.padding.right = 2;
        string contents = "";

        GUI.enabled = !settingApplied;

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
                contents = recommendedSetting.recommended;
                break;
            case Columns.Apply:
                if (GUI.Button(new Rect(cellRect.xMax - ((BUTTON_WIDTH / 2) * 2) - BUTTON_PADDING, cellRect.y + (BUTTON_HEIGHT / 4), BUTTON_WIDTH, BUTTON_HEIGHT), "Apply"))
                {
                    UltraleapSettings.Instance.ApplyRecommendedSetting(recommendedSettingKey);
                    Reload();
                    return;
                }
                break;

            case Columns.Ignore:
                GUI.enabled = true;
                if (recommendedSetting.ignored)
                {
                    if (GUI.Button(new Rect(cellRect.xMax - ((BUTTON_WIDTH / 2) * 2) - BUTTON_PADDING, cellRect.y + (BUTTON_HEIGHT / 4), BUTTON_WIDTH, BUTTON_HEIGHT), "Watch"))
                    {
                        recommendedSetting.ignored = false;
                        UltraleapSettings.Instance.recommendedSettings[recommendedSettingKey] = recommendedSetting;
                        Reload();
                        return;
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(cellRect.xMax - ((BUTTON_WIDTH / 2) * 2) - BUTTON_PADDING, cellRect.y + (BUTTON_HEIGHT / 4), BUTTON_WIDTH, BUTTON_HEIGHT), "Ignore"))
                    {
                        recommendedSetting.ignored = true;
                        UltraleapSettings.Instance.recommendedSettings[recommendedSettingKey] = recommendedSetting;
                        Reload();
                        return;
                    }
                }
                break;
        }


        if (column != Columns.Apply)
        {
            GUI.Label(cellRect, new GUIContent(contents, recommendedSetting.description), label);
        }
        GUI.enabled = true;

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
