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
    [CustomEditor(typeof(InteractionHand), editorForChildClasses: true)]
    public class InteractionHandEditor : InteractionControllerEditor
    {

        private Texture _handTex;
        private Rect _handTexRect;

        protected override void OnEnable()
        {
            base.OnEnable();

            _handTex = Resources.Load<Texture2D>("HandTex");

            hideField("_leapProvider");
            specifyCustomDecorator("manager", drawProvider);

            specifyCustomDrawer("enabledPrimaryHoverFingertips", drawPrimaryHoverFingertipsEditor);
        }

        private void drawProvider(SerializedProperty p)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_leapProvider"));
        }

        private void drawPrimaryHoverFingertipsEditor(SerializedProperty property)
        {

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(
              new GUIContent("Primary Hover Fingertips",
                             "Check which fingertips should be used "
                           + "as primary hover points for this interaction controller. Fewer "
                           + "points is cheaper. Proximity to one of these points determines "
                           + "which interaction object is chosen as the primary hover for this "
                           + "interaction controller at any given time. Generally speaking, "
                           + "choose the fingertips you'd like users to be able to use to "
                           + "choose and push a button."));

            Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.Height(102));
            drawHandEditor(controlRect);

            EditorGUILayout.EndHorizontal();
        }

        private void drawHandEditor(Rect controlRect)
        {
            // Determine whether the target object is a prefab. AttachmentPoints cannot be edited on prefabs.
            var isTargetPrefab = Leap.Unity.Utils.IsObjectPartOfPrefabAsset(target.gameObject);

            // Image container.
            Rect imageContainerRect = controlRect;
            EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));
            imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
            EditorGUI.DrawRect(imageContainerRect, new Color(0.6F, 0.6F, 0.6F));
            imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
            EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));

            // Hand image.
            _handTexRect = new Rect(0F, 0F, (controlRect.height - 2) * (_handTex.width / (float)_handTex.height), controlRect.height - 2);
            _handTexRect.center += imageContainerRect.center - _handTexRect.center;
            EditorGUI.DrawTextureTransparent(_handTexRect, _handTex);

            // Toggle boxes.
            EditorGUI.BeginDisabledGroup(isTargetPrefab);

            makeFingertipToggle(0, new Vector2(-0.390F, 0.110F));
            makeFingertipToggle(1, new Vector2(-0.080F, -0.380F));
            makeFingertipToggle(2, new Vector2(0.090F, -0.420F));
            makeFingertipToggle(3, new Vector2(0.245F, -0.380F));
            makeFingertipToggle(4, new Vector2(0.410F, -0.210F));
        }

        private void makeFingertipToggle(int fingerIndex, Vector2 offCenterPosImgSpace)
        {
            InteractionHand targetHand = target.intHand;
            InteractionHand[] targetHands = targets.Cast<InteractionHand>().ToArray();

            if (EditorGUI.Toggle(makeToggleRect(_handTexRect.center
                                                + new Vector2(offCenterPosImgSpace.x * _handTexRect.width,
                                                              offCenterPosImgSpace.y * _handTexRect.height)),

                                 targetHand.enabledPrimaryHoverFingertips[fingerIndex]))
            {
                foreach (var singleTarget in targetHands)
                {
                    serializedObject.FindProperty("enabledPrimaryHoverFingertips").GetArrayElementAtIndex(fingerIndex).boolValue = true;
                }
            }
            else
            {
                foreach (var singleTarget in targetHands)
                {
                    serializedObject.FindProperty("enabledPrimaryHoverFingertips").GetArrayElementAtIndex(fingerIndex).boolValue = false;
                }
            }
        }

        private const float TOGGLE_SIZE = 15.0F;
        private Rect makeToggleRect(Vector2 centerPos)
        {
            return new Rect(centerPos.x - TOGGLE_SIZE / 2F, centerPos.y - TOGGLE_SIZE / 2F, TOGGLE_SIZE, TOGGLE_SIZE);
        }

    }

}