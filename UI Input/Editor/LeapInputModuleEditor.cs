/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.InputModule
{
    [CustomEditor(typeof(LeapInputModule))]
    public class LeapInputModuleEditor : CustomEditorBase<LeapInputModule>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing(() => target.InteractionMode == LeapInputModule.InteractionCapability.Hybrid || target.InteractionMode == LeapInputModule.InteractionCapability.Projective,
                                     "pinchingThreshold",
                                     "environmentPointer",
                                     "environmentPinch",
                                     "pointerPinchScale",
                                     "leftHandDetector",
                                     "rightHandDetector",
                                     "hoveringColor");

            specifyConditionalDrawing(() => target.PointerSprite != null,
                               "pointerMaterial",
                               "standardColor",
                               "hoveringColor",
                               "triggeringColor",
                               "triggerMissedColor");

            specifyConditionalDrawing(() => target.InteractionMode == LeapInputModule.InteractionCapability.Hybrid || target.InteractionMode == LeapInputModule.InteractionCapability.Tactile,
                                     "tactilePadding");

            specifyConditionalDrawing(() => target.InteractionMode == LeapInputModule.InteractionCapability.Hybrid,
                                     "projectiveToTactileTransitionDistance",
                                     "retractUI");

            specifyConditionalDrawing(() => target.InnerPointer,
                               "innerPointerOpacityScalar");

            specifyConditionalDrawing(() => target.ShowAdvancedOptions,
                               "interactionMode",
                               "overrideScrollViewClicks",
                               "innerPointer",
                               "innerPointerOpacityScalar",
                               "drawDebug",
                               "triggerHoverOnElementSwitch",
                               "beginHoverSound",
                               "endHoverSound",
                               "beginTriggerSound",
                               "endTriggerSound",
                               "beginMissedSound",
                               "endMissedSound",
                               "dragLoopSound",
                               "onClickDown",
                               "onClickUp",
                               "onHover",
                               "whileClickHeld",
                               "projectiveToTactileTransitionDistance",
                               "pinchingThreshold",
                               "retractUI",
                               "tactilePadding",
                               "environmentPointer",
                               "perFingerPointer",
                               "showExperimentalOptions",
                               "pointerDistanceScale",
                               "pointerPinchScale",
                               "environmentPinch",
                               "movingReferenceFrame");

            specifyConditionalDrawing(() => target.ShowExperimentalOptions,
                         "interactionMode",
                         "pointerDistanceScale",
                         "pointerPinchScale",
                         "projectiveToTactileTransitionDistance",
                         "pinchingThreshold",
                         "innerPointer",
                         "innerPointerOpacityScalar",
                         "overrideScrollViewClicks",
                         "drawDebug",
                         "triggerHoverOnElementSwitch",
                         "perFingerPointer",
                         "retractUI",
                         "environmentPointer",
                         "environmentPinch",
                         "movingReferenceFrame");

            specifyConditionalDrawing(() => target.EnvironmentPointer,
                   "environmentPinch");
        }
    }
}
