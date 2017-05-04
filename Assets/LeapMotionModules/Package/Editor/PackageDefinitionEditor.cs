/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Leap.Unity.Packaging {

  [CustomEditor(typeof(PackageDefinition))]
  public class PackageDefinitionEditor : CustomEditorBase<PackageDefinition> {
    private List<PackageDefinition> _childPackages;

    protected override void OnEnable() {
      base.OnEnable();

      _childPackages = target.GetChildPackages();

      createList("_dependantFolders", drawFolderElement);
      createList("_dependantFiles", drawFileElement);
      createList("_dependantPackages", drawPackageElement);

      specifyCustomDrawer("_packageName", drawPackageName);
      specifyCustomDrawer("_generateBuildDropdown", drawGenerateBuildDropdown);

      specifyCustomDecorator("_dependantFolders", drawPackageExportFolder);
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

    private void drawPackageName(SerializedProperty property) {
      string newName = EditorGUILayout.DelayedTextField("Package Name", property.stringValue);
      string filteredName = new string(newName.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Trim();

      if (filteredName != "" && filteredName != property.stringValue) {
        property.stringValue = filteredName;

        if (target.GenerateBuildDropdown) {
          property.serializedObject.ApplyModifiedProperties();
          generateBuildMenuScript();
        }
      }
    }

    private void drawGenerateBuildDropdown(SerializedProperty property) {
      EditorGUI.BeginChangeCheck();
      EditorGUILayout.PropertyField(property);
      if (EditorGUI.EndChangeCheck()) {
        property.serializedObject.ApplyModifiedProperties();
        generateBuildMenuScript();
      }
    }

    private void drawPackageExportFolder(SerializedProperty property) {
      EditorGUILayout.BeginHorizontal();

      string folder;
      if (target.TryGetPackageExportFolder(out folder, promptIfNotDefined: false)) {
        EditorGUILayout.TextField("Package Export Folder", folder);
      } else {
        EditorGUILayout.LabelField("Package Export Folder");
      }

      if (GUILayout.Button("Change")) {
        target.PrompUserToSetExportPath();
      }

      EditorGUILayout.EndHorizontal();

      if (GUILayout.Button("Build Package", GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2))) {
        target.BuildPackage(interactive: true);
      }
      GUILayout.Space(EditorGUIUtility.singleLineHeight * 2);
    }

    private void drawFolderElement(Rect rect, SerializedProperty property) {
      drawExplorerElement(rect, property, EditorUtility.OpenFolderPanel);
    }

    private void drawFileElement(Rect rect, SerializedProperty property) {
      drawExplorerElement(rect, property, EditorUtility.OpenFilePanel);
    }

    private void drawPackageElement(Rect rect, SerializedProperty property) {
      EditorGUI.PropertyField(rect, property, GUIContent.none);
    }

    private ReorderableList createList(string propertyName, Action<Rect, SerializedProperty> drawMethod) {
      SerializedProperty listProperty = serializedObject.FindProperty(propertyName);

      var list = new ReorderableList(serializedObject, listProperty,
                                     draggable: true,
                                     displayHeader: true,
                                     displayAddButton: true,
                                     displayRemoveButton: true);

      list.drawElementCallback += (rect, index, isActive, isFocused) => {
        SerializedProperty property = list.serializedProperty.GetArrayElementAtIndex(index);
        drawMethod(rect, property);
      };

      list.drawHeaderCallback += (rect) => {
        GUI.Label(rect, listProperty.displayName);
      };

      list.elementHeight = EditorGUIUtility.singleLineHeight;


      specifyCustomDrawer(propertyName, p => list.DoLayoutList());

      return list;
    }

    private void drawExplorerElement(Rect rect, SerializedProperty property, Func<string, string, string, string> openAction) {
      Rect leftRect = rect;
      Rect rightRect = rect;

      leftRect.width -= 100;

      rightRect.x = leftRect.x + leftRect.width;
      rightRect.width = 100;

      EditorGUI.TextField(leftRect, property.stringValue);

      if (GUI.Button(rightRect, "Change")) {
        string chosenFolder = openAction("Select Folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(chosenFolder)) {
          string relativePath = makeRelativePath(Application.dataPath, chosenFolder);
          if (!string.IsNullOrEmpty(relativePath) && !relativePath.StartsWith("..")) {
            property.stringValue = relativePath;
          }
        }
      }
    }

    private static string makeRelativePath(string fromPath, string toPath) {
      if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
      if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

      Uri fromUri = new Uri(fromPath);
      Uri toUri = new Uri(toPath);

      if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

      Uri relativeUri = fromUri.MakeRelativeUri(toUri);
      string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

      if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase)) {
        relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      }

      return relativePath;
    }

    private void generateBuildMenuScript() {
      var definitions = PackageDefinition.FindAll();

      StringBuilder builder = new StringBuilder();
      builder.AppendLine("using UnityEditor;");
      builder.AppendLine();
      builder.AppendLine("namespace Leap.Unity.Packaging {");
      builder.AppendLine();
      builder.AppendLine("  public class PackageDefinitionBuildMenuItems {");

      foreach (var def in definitions) {
        if (!def.GenerateBuildDropdown) continue;

        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(def));

        builder.AppendLine("");
        builder.AppendLine("    // " + def.PackageName);
        builder.AppendLine("    [MenuItem(\"Build/" + def.PackageName + "\")]");
        builder.AppendLine("    public static void Build_" + guid + "() {");
        builder.AppendLine("      PackageDefinition.BuildPackage(\"" + guid + "\");");
        builder.AppendLine("    }");
      }

      builder.AppendLine("  }");
      builder.AppendLine("}");
      builder.AppendLine();

      File.WriteAllText("Assets/LeapMotionModules/Package/Editor/PackageDefinitionBuildMenuItems.cs", builder.ToString());
      AssetDatabase.Refresh();
    }
  }
}
