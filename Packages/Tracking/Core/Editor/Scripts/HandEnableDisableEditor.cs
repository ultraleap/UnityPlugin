/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;

namespace Leap
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandEnableDisable))]
    public class HandEnableDisableEditor : CustomEditorBase<HandEnableDisable>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing("fadeOnHandFound",
                          "fadeInTime");

            specifyConditionalDrawing("fadeOnHandLost",
              "fadeOutTime");

            specifyConditionalDrawing(() =>
            {
                return target.fadeOnHandFound || target.fadeOnHandLost;
            }, "customFadeRenderers");

            specifyConditionalDrawing(() =>
            {
                return (target.fadeOnHandFound || target.fadeOnHandLost) && target.customFadeRenderers;
            }, "renderersToFade");
        }
    }
}