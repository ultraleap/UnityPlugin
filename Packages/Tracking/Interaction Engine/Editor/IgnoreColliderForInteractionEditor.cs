/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction
{

    [CustomEditor(typeof(IgnoreColliderForInteraction))]
    public class IgnoreColliderForInteractionEditor
      : CustomEditorBase<IgnoreColliderForInteraction>
    {


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.HelpBox(
              "Causes any Colliders located on the same GameObject to be ignored by the "
            + "Interaction Engine. Does not affect parents or children. It is recommended that "
            + "you use this component only with trigger colliders.",
              MessageType.None);
        }

    }

}