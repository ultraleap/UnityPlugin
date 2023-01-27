/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.Preview.XRInteractionToolkit.Editor
{
    [CustomEditor(typeof(TrackedHandsController))]
    public class TrackedHandsControllerEditor : CustomEditorBase<TrackedHandsController>
    {
        protected override void OnEnable()
        {

            base.OnEnable();

            hideField("m_UpdateTrackingType");
            hideField("m_EnableInputTracking");
            hideField("m_EnableInputActions");
            hideField("m_ModelPrefab");
            hideField("m_ModelParent");
            hideField("m_Model");
            hideField("m_AnimateModel");
            hideField("m_ModelSelectTransition");
            hideField("m_ModelDeSelectTransition");
        }
    }
}