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

      specifyConditionalDrawing("_useCustomLayers", 
                                "_interactionLayer", 
                                "_interactionNoClipLayer");
    }
  }
}
