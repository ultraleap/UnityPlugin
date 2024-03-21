/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using UnityEditor;

namespace Leap.InteractionEngine.Examples.Editor
{

    [CustomEditor(typeof(TransformHandle), true)]
    [CanEditMultipleObjects]
    public class TransformHandleEditor : CustomEditorBase<TransformHandle>
    {

        protected override void OnEnable()
        {
            base.OnEnable();

            deferProperty("OnHandleDeactivated");
            deferProperty("OnHandleActivated");
            deferProperty("OnShouldHideHandle");
            deferProperty("OnShouldShowHandle");
        }

    }

}