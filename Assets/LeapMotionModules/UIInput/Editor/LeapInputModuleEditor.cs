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

      specifyConditionalDrawing(() => module.InteractionMode == LeapInputModule.InteractionCapability.Hybrid || module.InteractionMode == LeapInputModule.InteractionCapability.Projective,
                               "PinchingThreshold",
                               "EnvironmentPointer",
                               "environmentPinch",
                               "PointerPinchScale",
                               "LeftHandDetector",
                               "RightHandDetector",
                               "HoveringColor");

      specifyConditionalDrawing(() => module.InteractionMode == LeapInputModule.InteractionCapability.Hybrid || module.InteractionMode == LeapInputModule.InteractionCapability.Tactile,
                               "TactilePadding");

      specifyConditionalDrawing(() => module.InteractionMode == LeapInputModule.InteractionCapability.Hybrid,
                               "ProjectiveToTactileTransitionDistance",
                               "RetractUI");

      specifyConditionalDrawing(() => module.InnerPointer,
                         "InnerPointerOpacityScalar");

      specifyConditionalDrawing(() => module.ShowAdvancedOptions,
                         "InteractionMode",
                         "OverrideScrollViewClicks",
                         "InnerPointer",
                         "InnerPointerOpacityScalar",
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
                         "TactilePadding",
                         "EnvironmentPointer",
                         "perFingerPointer",
                         "ShowExperimentalOptions",
                         "PointerDistanceScale",
                         "PointerPinchScale",
                         "environmentPinch");

      specifyConditionalDrawing(() => module.ShowExperimentalOptions,
                   "InteractionMode",
                   "PointerDistanceScale",
                   "PointerPinchScale",
                   "ProjectiveToTactileTransitionDistance",
                   "PinchingThreshold",
                   "InnerPointer",
                   "InnerPointerOpacityScalar",
                   "OverrideScrollViewClicks",
                   "DrawDebug",
                   "TriggerHoverOnElementSwitch",
                   "perFingerPointer",
                   "RetractUI",
                   "EnvironmentPointer",
                   "environmentPinch");

      specifyConditionalDrawing(() => module.EnvironmentPointer,
             "environmentPinch");
    }
  }
}