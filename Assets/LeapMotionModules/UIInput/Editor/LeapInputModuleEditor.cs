using UnityEngine;
using UnityEditor;
using Leap.Unity;
using System.Collections;

namespace Leap.Unity.InputModule {
  [CustomEditor(typeof(LeapInputModule))]
  public class LeapInputModuleEditor : CustomEditorBase {
    protected override void OnEnable() {
      base.OnEnable();
      LeapInputModule module = target as LeapInputModule;

      specifyConditionalDrawing(() => ProjectiveAllowed(module.InteractionMode),
                               "ProjectiveToTactileTransitionDistance",
                               "PinchingThreshold");
    }

    bool ProjectiveAllowed(LeapInputModule.InteractionCapability mode) {
      return mode != LeapInputModule.InteractionCapability.Tactile;
    }
  }
}