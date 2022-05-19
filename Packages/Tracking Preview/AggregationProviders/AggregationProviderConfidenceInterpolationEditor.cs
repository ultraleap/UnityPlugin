/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{

    [CustomEditor(typeof(AggregationProviderConfidenceInterpolation))]
    public class AggregationProviderConfidenceInterpolationEditor : CustomEditorBase
    {
        
        protected override void OnEnable()
        {
            base.OnEnable();

            specifyCustomDrawer("jointOcclusionFactor", drawJointOcclusionWarning);
        }

        private void drawJointOcclusionWarning(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, true);

            if (property.floatValue != 0)
            {
                EditorGUILayout.HelpBox("If you want to use jointOcclusion, you need to add a Layers named 'JointOcclusion[idx]'. One for each provider in the providers list above. (eg. Add 'JointOcclusion0' and 'JointOcclusion1' when using two source providers.)", MessageType.Warning);
            }
        }


        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();
        }

    }
}