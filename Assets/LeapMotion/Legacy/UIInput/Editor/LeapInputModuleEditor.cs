/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.InputModule {
  [CustomEditor(typeof(LeapInputModule))]
  public class LeapInputModuleEditor : CustomEditorBase<LeapInputModule> {
    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing(() => target.InteractionMode == LeapInputModule.InteractionCapability.Hybrid || target.InteractionMode == LeapInputModule.InteractionCapability.Projective,
                               "PinchingThreshold",
                               "EnvironmentPointer",
                               "environmentPinch",
                               "PointerPinchScale",
                               "LeftHandDetector",
                               "RightHandDetector",
                               "HoveringColor");

      specifyConditionalDrawing(() => target.PointerSprite != null,
                         "PointerMaterial",
                         "StandardColor",
                         "HoveringColor",
                         "TriggeringColor",
                         "TriggerMissedColor");

      specifyConditionalDrawing(() => target.InteractionMode == LeapInputModule.InteractionCapability.Hybrid || target.InteractionMode == LeapInputModule.InteractionCapability.Tactile,
                               "TactilePadding");

      specifyConditionalDrawing(() => target.InteractionMode == LeapInputModule.InteractionCapability.Hybrid,
                               "ProjectiveToTactileTransitionDistance",
                               "RetractUI");

      specifyConditionalDrawing(() => target.InnerPointer,
                         "InnerPointerOpacityScalar");

      specifyConditionalDrawing(() => target.ShowAdvancedOptions,
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
                         "environmentPinch",
                         "MovingReferenceFrame");

      specifyConditionalDrawing(() => target.ShowExperimentalOptions,
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
                   "environmentPinch",
                   "MovingReferenceFrame");

      specifyConditionalDrawing(() => target.EnvironmentPointer,
             "environmentPinch");
    }
  }
}
