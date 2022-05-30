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

            drawFoldoutInLine();

            addPropertyToFoldout("palmPosFactor", "Palm Factors");
            addPropertyToFoldout("palmRotFactor", "Palm Factors");
            addPropertyToFoldout("palmVelocityFactor", "Palm Factors");

            addPropertyToFoldout("jointRotFactor", "Joint Factors");
            addPropertyToFoldout("jointRotToPalmFactor", "Joint Factors");
            addPropertyToFoldout("jointOcclusionFactor", "Joint Factors");
        }

        private void drawJointOcclusionWarning(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, true);

            if (property.floatValue != 0)
            {
                EditorGUILayout.HelpBox("To use jointOcclusion, you must add a Layer for each ServiceProvider named 'JointOcclusion[idx]'. (eg. Add 'JointOcclusion0' and 'JointOcclusion1' when using two source providers.)", MessageType.Warning);
            }
        }


        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();
        }

    }
}