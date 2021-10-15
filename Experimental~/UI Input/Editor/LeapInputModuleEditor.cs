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

            specifyConditionalDrawing(() => target.InteractionMode == InteractionCapability.Both || target.InteractionMode == InteractionCapability.Indirect,
                "pinchingThreshold",
                                     "pointerPinchScale",
                                     "leftHandDetector",
                                     "rightHandDetector",
                                     "hoveringColor");
            
            specifyConditionalDrawing(
                () => target.InteractionMode == InteractionCapability.Both ||
                      target.InteractionMode == InteractionCapability.Direct,
                "tactilePadding");

            specifyConditionalDrawing(() => target.InteractionMode == InteractionCapability.Both,
                "projectiveToTactileTransitionDistance");
            
            //Calibration Setup
            addPropertyToFoldout("pinchingThreshold", "Calibration Setup");
            addPropertyToFoldout("tactilePadding", "Calibration Setup");
            addPropertyToFoldout("projectiveToTactileTransitionDistance", "Calibration Setup");
            addPropertyToFoldout("triggerHoverOnElementSwitch", "Calibration Setup");
            addPropertyToFoldout("movingReferenceFrame","Calibration Setup");
            
            //Pointer Setup
            addPropertyToFoldout("pointerSprite", "Pointer Setup");
            addPropertyToFoldout("pointerDistanceScale", "Pointer Setup");
            addPropertyToFoldout("pointerPinchScale","Pointer Setup");
            addPropertyToFoldout("pointerMaterial","Pointer Setup");
            addPropertyToFoldout("standardColor","Pointer Setup");
            addPropertyToFoldout("hoveringColor","Pointer Setup");
            addPropertyToFoldout("triggeringColor","Pointer Setup");
            addPropertyToFoldout("triggerMissedColor","Pointer Setup");
            addPropertyToFoldout("innerPointer", "Pointer Setup");
            addPropertyToFoldout("innerPointerOpacityScalar", "Pointer Setup");
            
            specifyConditionalDrawing(() => target.PointerSprite != null,
                "pointerDistanceScale",
                "pointerPinchScale",
                "pointerMaterial",
                "standardColor",
                "hoveringColor",
                "triggeringColor",
                "triggerMissedColor",
                "innerPointer");

            specifyConditionalDrawing(() => target.PointerSprite != null && target.InnerPointer,
                "innerPointerOpacityScalar");
        }
    }
}
