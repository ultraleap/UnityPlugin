using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Collections.Generic;

namespace Leap.Unity.Packaging {

  [CustomEditor(typeof(PackageDefinition))]
  public class PackageDefinitionEditor : CustomEditorBase {

    private PackageDefinition _def;
    private List<PackageDefinition> _parentPackages;

    protected override void OnEnable() {
      base.OnEnable();

      _def = target as PackageDefinition;

      _parentPackages = _def.GetParentPackages();

      createList("_dependantFolders", drawFolderElement);
      createList("_dependantFiles", drawFileElement);
      createList("_dependantPackages", drawPackageElement);

      specifyCustomDecorator("_dependantFolders", drawPackageExportFolder);
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (_parentPackages.Count != 0) {
        GUILayout.Space(EditorGUIUtility.singleLineHeight * 2);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Packages that depend on this package", EditorStyles.boldLabel);
        if (GUILayout.Button("Build All")) {
          _def.BuildAllParentPackages();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(true);
        foreach (var parentPackage in _parentPackages) {
          EditorGUILayout.ObjectField(parentPackage, typeof(PackageDefinition), false);
        }
        EditorGUI.EndDisabledGroup();
        
      }
    }

    private void drawPackageExportFolder(SerializedProperty property) {
      EditorGUILayout.BeginHorizontal();

      string folder;
      if (_def.TryGetPackageExportFolder(out folder, promptIfNotDefined: false)) {
        EditorGUILayout.TextField("Package Export Folder", folder);
      } else {
        EditorGUILayout.LabelField("Package Export Folder");
      }

      if (GUILayout.Button("Change")) {
        _def.PrompUserToSetExportPath();
      }

      EditorGUILayout.EndHorizontal();

      if (GUILayout.Button("Build Package", GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2))) {
        _def.BuildPackage(interactive: true);
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
        string relativePath = makeRelativePath(Application.dataPath, chosenFolder);
        if (!string.IsNullOrEmpty(relativePath) && !relativePath.StartsWith("..")) {
          property.stringValue = relativePath;
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
  }
}
