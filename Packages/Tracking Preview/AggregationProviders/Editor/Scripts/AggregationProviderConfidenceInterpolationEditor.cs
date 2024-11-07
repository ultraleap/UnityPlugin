/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap
{

    [CustomEditor(typeof(AggregationProviderConfidenceInterpolation))]
    public class AggregationProviderConfidenceInterpolationEditor : CustomEditorBase<AggregationProviderConfidenceInterpolation>
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

            specifyConditionalDrawing("debugJointOrigins",
                                        "debugHandLeft",
                                        "debugHandRight",
                                        "debugColors");
        }

        private void drawJointOcclusionWarning(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, true);


            if (property.floatValue != 0)
            {
                bool showWarning = false;
                string warningText = "To use jointOcclusion, you must add the following Layers: ";
                for (int i = 0; i < target.providers.Length; i++)
                {
                    string layerName = "JointOcclusion" + i.ToString();
                    if (LayerMask.NameToLayer(layerName) == -1)
                    {
                        showWarning = true;
                        warningText += layerName + ", ";
                    }
                }

                if (showWarning)
                {
                    EditorGUILayout.HelpBox(warningText.TrimEnd(' ', ','), MessageType.Warning);
                }
            }
        }


        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();
        }

    }
}