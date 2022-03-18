/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Leap.Unity.Preview.XRInteractionToolkit
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