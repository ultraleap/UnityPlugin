/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(InteractionSlider), editorForChildClasses: true)]
    public class InteractionSliderEditor : InteractionButtonEditor
    {

        protected override void OnEnable()
        {
            base.OnEnable();

            // specifyConditionalDrawing(() => noRectTransformParent, "horizontalSlideLimits", "verticalSlideLimits");

            specifyCustomDecorator("horizontalSlideLimits", decorateHorizontalSlideLimits);
            specifyCustomDecorator("verticalSlideLimits", decorateVerticalSlideLimits);
            specifyCustomDecorator("horizontalSteps", decorateHorizontalSteps);

            // Only display vertical properties if relevant
            InteractionSlider[] sliders = targets.Cast<InteractionSlider>().ToArray();
            specifyConditionalDrawing(() =>
            {
                return sliders.Any(slider => slider.sliderType == InteractionSlider.SliderType.Vertical
                                                  || slider.sliderType == InteractionSlider.SliderType.TwoDimensional);
            },
                                      "defaultVerticalValue",
                                      "_verticalValueRange",
                                      "verticalSlideLimits",
                                      "verticalSteps",
                                      "_verticalSlideEvent");
            specifyConditionalDrawing(() =>
            {
                return sliders.Any(slider => slider.sliderType == InteractionSlider.SliderType.Horizontal
                                                  || slider.sliderType == InteractionSlider.SliderType.TwoDimensional);
            },
                                      "defaultHorizontalValue",
                                      "_horizontalValueRange",
                                      "horizontalSlideLimits",
                                      "horizontalSteps",
                                      "_horizontalSlideEvent");
        }

        public override void OnInspectorGUI()
        {
            bool noRectTransformParent = !(target.transform.parent != null && target.transform.parent.GetComponent<RectTransform>() != null && !(target as InteractionSlider).overrideRectLimits);
            if (!noRectTransformParent)
            {
                EditorGUILayout.HelpBox("This slider's limits are being controlled by the rect transform in its parent.", MessageType.Info);
            }

            if (!Application.isPlaying)
            {
                (target as InteractionSlider).RecalculateSliderLimits();
            }

            base.OnInspectorGUI();
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        private void decorateHorizontalSlideLimits(SerializedProperty property)
        {
            EditorGUI.BeginDisabledGroup(target.transform.parent != null && target.transform.parent.GetComponent<RectTransform>() != null && !(target as InteractionSlider).overrideRectLimits);
        }

        private void decorateVerticalSlideLimits(SerializedProperty property)
        {
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(target.transform.parent != null && target.transform.parent.GetComponent<RectTransform>() != null && !(target as InteractionSlider).overrideRectLimits);
        }

        private void decorateHorizontalSteps(SerializedProperty property)
        {
            EditorGUI.EndDisabledGroup();
        }

    }
}