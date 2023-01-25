/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.InputModule
{
    [CustomEditor(typeof(UIInputModule))]
    public class UIInputModuleEditor : CustomEditorBase<UIInputModule>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing(() => target.InteractionMode == InteractionCapability.Both || target.InteractionMode == InteractionCapability.Indirect,
                "pinchingThreshold");

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
            addPropertyToFoldout("movingReferenceFrame", "Calibration Setup");
        }
    }
}