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

      SerializedProperty graspMethod = serializedObject.FindProperty("_graspMethod");
      specifyConditionalDrawing(() => graspMethod.intValue == (int)InteractionMaterial.GraspMethodEnum.Velocity,
                                "_releaseDistance",
                                "_maxVelocity",
                                "_followStrength");
    }
  }
}
