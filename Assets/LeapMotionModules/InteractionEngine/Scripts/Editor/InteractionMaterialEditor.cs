using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionMaterial))]
  public class InteractionMaterialEditor : CustomEditorBase {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing("_warpingEnabled",
                                "_warpCurve",
                                "_graphicalReturnTime");

      specifyConditionalDrawing("_contactEnabled",
                                "_brushDisableDistance");

      specifyConditionalDrawing("_suspensionEnabled",
                                "_maxSuspensionTime",
                                "_hideObjectOnSuspend");

      specifyConditionalDrawing("_graspingEnabled",
                                "_graspMethod",
                                "_releaseDistance",
                                "_maxVelocity",
                                "_strengthByDistance",
                                "_throwingVelocityCurve",
                                "_suspensionEnabled",
                                "_maxSuspensionTime",
                                "_hideObjectOnSuspend",
                                "_warpingEnabled",
                                "_warpCurve",
                                "_graphicalReturnTime");

      SerializedProperty graspMethod = serializedObject.FindProperty("_graspMethod");
      specifyConditionalDrawing(() => graspMethod.intValue == (int)InteractionMaterial.GraspMethodEnum.Velocity,
                                "_releaseDistance",
                                "_maxVelocity",
                                "_strengthByDistance");

      SerializedProperty physicMaterialMode = serializedObject.FindProperty("_physicMaterialMode");
      specifyConditionalDrawing(() => physicMaterialMode.intValue == (int)InteractionMaterial.PhysicMaterialModeEnum.Replace,
                                "_replacementMaterial");

      specifyCustomDecorator("_replacementMaterial", replacementMaterialDecorator);

      specifyConditionalDrawing("_useCustomLayers",
                                "_interactionLayer",
                                "_interactionNoClipLayer");
    }

    private void replacementMaterialDecorator(SerializedProperty prop) {
      PhysicMaterial mat = prop.objectReferenceValue as PhysicMaterial;
      if (mat != null && mat.bounciness > 0) {
        using (new GUILayout.HorizontalScope()) {
          EditorGUILayout.HelpBox("Replacement material should have boinciness of zero", MessageType.Error);
          if (GUILayout.Button("Auto-Fix")) {
            mat.bounciness = 0;
          }
        }
      }
    }
  }
}
