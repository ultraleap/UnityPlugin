/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Leap.Unity.Packaging {

  [CustomEditor(typeof(DefinitionBase))]
  public abstract class DefinitionBaseEditor<T> : CustomEditorBase<T> where T : DefinitionBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("_definitionName", drawDefName);
      specifyCustomDrawer("_showInBuildMenu", drawGenerateBuildDropdown);
    }

    protected abstract void OnBuild();

    protected abstract int GetBuildMenuPriority();

    protected abstract string GetBuildMethodName();

    protected void drawExportFolder(SerializedProperty prop, string buildText, string label) {
      EditorGUILayout.BeginHorizontal();

      string folder;
      if (target.TryGetPackageExportFolder(out folder, promptIfNotDefined: false)) {
        EditorGUILayout.TextField(label, folder);
      } else {
        EditorGUILayout.LabelField(label);
      }

      if (GUILayout.Button("Change", GUILayout.ExpandWidth(false))) {
        target.PrompUserToSetExportPath();
      }

      EditorGUILayout.EndHorizontal();

      if (GUILayout.Button(buildText, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 2))) {
        EditorApplication.delayCall += () => OnBuild();
      }
      GUILayout.Space(EditorGUIUtility.singleLineHeight * 2);
    }

    private void drawDefName(SerializedProperty property) {
      string newName = EditorGUILayout.DelayedTextField("Package Name", property.stringValue);
      string filteredName = new string(newName.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Trim();

      if (filteredName != "" && filteredName != property.stringValue) {
        property.stringValue = filteredName;

        if (target.ShowInBuildMenu) {
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

    private void generateBuildMenuScript() {
      string typeName = typeof(T).Name;

      var definitions = AssetDatabase.FindAssets("t:" + typeName).
                                      Select(guid => AssetDatabase.GUIDToAssetPath(guid)).
                                      Select(path => AssetDatabase.LoadAssetAtPath<DefinitionBase>(path)).
                                      OrderBy(def => def.DefinitionName).
                                      ToArray();

      StringBuilder builder = new StringBuilder();
      builder.AppendLine("using UnityEditor;");
      builder.AppendLine();
      builder.AppendLine("namespace Leap.Unity.Packaging {");
      builder.AppendLine();
      builder.AppendLine("  public class " + typeName + "BuildMenuItems { ");

      foreach (var def in definitions) {
        if (!def.ShowInBuildMenu) continue;

        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(def));

        builder.AppendLine("");
        builder.AppendLine("    // " + def.DefinitionName);
        builder.AppendLine("    [MenuItem(\"Build/" + def.DefinitionName + "\", priority = " + GetBuildMenuPriority() + ")]");
        builder.AppendLine("    public static void Build_" + guid + "() {");
        builder.AppendLine("      " + typeName + "." + GetBuildMethodName() + "(\"" + guid + "\");");
        builder.AppendLine("    }");
      }

      builder.AppendLine("  }");
      builder.AppendLine("}");
      builder.AppendLine();

      File.WriteAllText("Assets/LeapMotion/Modules/Package/Editor/" + typeName + "BuildMenuItems.cs", builder.ToString());
      AssetDatabase.Refresh();
    }

    protected ReorderableList createList(string propertyName, Action<Rect, SerializedProperty> drawMethod) {
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
  }
}
