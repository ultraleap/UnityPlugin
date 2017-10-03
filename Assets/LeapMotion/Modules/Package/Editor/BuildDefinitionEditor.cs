using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Packaging {

  [CustomEditor(typeof(BuildDefinition))]
  public class BuildDefinitionEditor : DefinitionBaseEditor<BuildDefinition> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDecorator("_options", prop => drawExportFolder(prop, "Build", "Build Folder"));

      createList("_scenes", drawScene);
      createList("_targets", drawBuildTarget);
    }

    protected override void OnBuild() {
      target.Build();
    }

    protected override int GetBuildMenuPriority() {
      return 20;
    }

    protected override string GetBuildMethodName() {
      return "Build";
    }

    private void drawScene(Rect rect, SerializedProperty property) {
      float originalWidth = EditorGUIUtility.labelWidth;
      EditorGUIUtility.labelWidth *= 0.2f;

      string label = new string(property.displayName.Where(c => char.IsDigit(c)).ToArray());
      EditorGUI.PropertyField(rect, property, new GUIContent(label));

      EditorGUIUtility.labelWidth = originalWidth;
    }

    private void drawBuildTarget(Rect rect, SerializedProperty property) {
      EditorGUI.PropertyField(rect, property, GUIContent.none);
    }
  }
}
