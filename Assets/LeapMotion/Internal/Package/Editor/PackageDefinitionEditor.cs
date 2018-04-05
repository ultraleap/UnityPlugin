/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Leap.Unity.Packaging {

  [CustomEditor(typeof(PackageDefinition))]
  public class PackageDefinitionEditor : DefinitionBaseEditor<PackageDefinition> {
    private List<PackageDefinition> _childPackages;

    private SerializedProperty _ignoredFiles;
    private SerializedProperty _ignoredFolders;

    protected override void OnEnable() {
      base.OnEnable();

      _childPackages = target.GetChildPackages();

      _ignoredFiles = serializedObject.FindProperty("_ignoredFiles");
      _ignoredFolders = serializedObject.FindProperty("_ignoredFolders");

      hideField("_ignoredFolders");
      hideField("_ignoredFiles");

      createList("_dependantFolders", drawFolderElement);
      createList("_dependantFiles", drawFileElement);
      createList("_dependantPackages", drawPackageElement);

      specifyCustomDecorator("_dependantFolders", prop => drawExportFolder(prop, "Build Package", "Package Export Folder"));
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (_childPackages.Count != 0) {
        GUILayout.Space(EditorGUIUtility.singleLineHeight * 2);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Packages that depend on this package", EditorStyles.boldLabel);
        if (GUILayout.Button("Build All")) {
          target.BuildAllChildPackages();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(true);
        foreach (var childPackage in _childPackages) {
          EditorGUILayout.ObjectField(childPackage, typeof(PackageDefinition), false);
        }
        EditorGUI.EndDisabledGroup();

      }
    }

    protected override void OnBuild() {
      target.BuildPackage(interactive: true);
    }

    protected override int GetBuildMenuPriority() {
      return 50;
    }

    protected override string GetBuildMethodName() {
      return "BuildPackage";
    }

    private void drawFolderElement(Rect rect, SerializedProperty property) {
      drawExplorerElement(rect, property, _ignoredFolders, EditorUtility.OpenFolderPanel);
    }

    private void drawFileElement(Rect rect, SerializedProperty property) {
      drawExplorerElement(rect, property, _ignoredFiles, EditorUtility.OpenFilePanel);
    }

    private void drawPackageElement(Rect rect, SerializedProperty property) {
      EditorGUI.PropertyField(rect, property, GUIContent.none);
    }

    private void drawExplorerElement(Rect rect, SerializedProperty property, SerializedProperty ignoredList, Func<string, string, string, string> openAction) {
      Rect left, middle, right;

      rect.SplitHorizontallyWithRight(out rect, out right, 100);
      rect.SplitHorizontallyWithRight(out left, out middle, EditorGUIUtility.singleLineHeight);

      EditorGUI.TextField(left, property.stringValue);

      bool isIncluded = true;
      for (int i = 0; i < ignoredList.arraySize; i++) {
        if (ignoredList.GetArrayElementAtIndex(i).stringValue == property.stringValue) {
          isIncluded = false;
          break;
        }
      }

      bool shouldBeIncluded = EditorGUI.Toggle(middle, isIncluded);

      if (shouldBeIncluded != isIncluded) {
        if (shouldBeIncluded) {
          for (int i = ignoredList.arraySize; i-- != 0;) {
            if (ignoredList.GetArrayElementAtIndex(i).stringValue == property.stringValue) {
              ignoredList.DeleteArrayElementAtIndex(i);
            }
          }
        } else {
          ignoredList.InsertArrayElementAtIndex(0);
          ignoredList.GetArrayElementAtIndex(0).stringValue = property.stringValue;
        }
      }

      if (GUI.Button(right, "Change")) {
        string chosenFolder = openAction("Select Folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(chosenFolder)) {
          string relativePath = Utils.MakeRelativePath(Application.dataPath, chosenFolder);
          if (!string.IsNullOrEmpty(relativePath) && !relativePath.StartsWith("..")) {
            if (relativePath != property.stringValue) {
              property.stringValue = relativePath;
            }
          }
        }
      }
    }
  }
}
