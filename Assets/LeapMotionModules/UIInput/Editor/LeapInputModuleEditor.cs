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
                               "PinchingThreshold");

      specifyConditionalDrawing(() => module.InteractionMode == LeapInputModule.InteractionCapability.Hybrid,
                               "ProjectiveToTactileTransitionDistance");

      specifyConditionalDrawing(() => module.ShowAdvancedOptions,
                         "InteractionMode",
                         "OverrideScrollViewClicks",
                         "DrawDebug",
                         "TriggerHoverOnElementSwitch",
                         "BeginHoverSound",
                         "EndHoverSound",
                         "BeginTriggerSound",
                         "EndTriggerSound",
                         "BeginMissedSound",
                         "EndMissedSound",
                         "DragLoopSound",
                         "onClickDown",
                         "onClickUp",
                         "onHover",
                         "whileClickHeld",
                         "ProjectiveToTactileTransitionDistance",
                         "PinchingThreshold",
                         "RetractUI",
                         "ShowExperimentalOptions");

      specifyConditionalDrawing(() => module.ShowExperimentalOptions,
                   "OverrideScrollViewClicks",
                   "DrawDebug",
                   "TriggerHoverOnElementSwitch",
                   "RetractUI");
    }

    bool ProjectiveAllowed(LeapInputModule.InteractionCapability mode) {
      return mode != LeapInputModule.InteractionCapability.Tactile;
    }
  }
}