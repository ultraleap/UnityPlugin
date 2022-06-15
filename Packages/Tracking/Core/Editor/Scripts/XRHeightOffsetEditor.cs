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

namespace Leap.Unity
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(XRHeightOffset))]
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class XRHeightOffsetEditor : CustomEditorBase<XRHeightOffset>
    {

        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing(conditionalName: "recenterOnKey",
                                      dependantProperties: "recenterKey");
        }

    }

}