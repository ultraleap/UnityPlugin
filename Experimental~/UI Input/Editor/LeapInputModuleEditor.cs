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

            specifyConditionalDrawing(() => target.InteractionMode == InteractionCapability.Hybrid || target.InteractionMode == InteractionCapability.Projective,
                                     "pinchingThreshold",
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

            specifyConditionalDrawing(() => target.InteractionMode == InteractionCapability.Hybrid || target.InteractionMode == InteractionCapability.Tactile,
                                     "tactilePadding");

            specifyConditionalDrawing(() => target.InteractionMode == InteractionCapability.Hybrid,
                                     "projectiveToTactileTransitionDistance",
                                     "retractUI");

            specifyConditionalDrawing(() => target.InnerPointer,
                               "innerPointerOpacityScalar");
        }
    }
}
