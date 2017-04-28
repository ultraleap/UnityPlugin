using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity {

  [CustomEditor(typeof(AttachmentHands))]
  public class AttachmentHandsEditor : CustomEditorBase<AttachmentHands> {

    private Texture _handTex;
    private Rect _handTexRect;

    private SerializedProperty _attachmentPointsProperty;

    protected override void OnEnable() {
      base.OnEnable();

      _handTex = EditorResources.Load<Texture2D>("HandTex");

      this.specifyCustomDrawer("_attachmentPoints", drawAttachmentPointsEditor);
    }

    private void drawAttachmentPointsEditor(SerializedProperty property) {
      // Set up the draw rect space based on the image and available editor space.
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Attachment Transforms", EditorStyles.boldLabel);
      _handTexRect = EditorGUILayout.BeginVertical(GUILayout.MinWidth(EditorGUIUtility.currentViewWidth),
                                                   GUILayout.MinHeight(EditorGUIUtility.currentViewWidth * (_handTex.height / (float)_handTex.width)),
                                                   GUILayout.MaxWidth(_handTex.width),
                                                   GUILayout.MaxHeight(_handTex.height));
      
      Rect imageContainerRect = _handTexRect; imageContainerRect.width = EditorGUIUtility.currentViewWidth - 30F;
      EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));
      imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
      EditorGUI.DrawRect(imageContainerRect, new Color(0.6F, 0.6F, 0.6F));
      imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
      EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));

      _handTexRect = new Rect(_handTexRect.x + (imageContainerRect.center.x - _handTexRect.center.x),
                              _handTexRect.y,
                              _handTexRect.width,
                              _handTexRect.height);
      EditorGUI.DrawTextureTransparent(_handTexRect, _handTex);
      EditorGUILayout.Space();

      _attachmentPointsProperty = property;


      // Draw the toggles for the attachment points.

      makeAttachmentPointsToggle("palm", new Vector2(0.09F, 0.15F));
      makeAttachmentPointsToggle("wrist", new Vector2(0.07F, 0.42F));

      makeAttachmentPointsToggle("thumbProximalJoint", new Vector2(-0.20F, 0.25F));
      makeAttachmentPointsToggle("thumbDistalJoint", new Vector2(-0.32F, 0.16F));
      makeAttachmentPointsToggle("thumbTip", new Vector2(-0.4F, 0.1F));

      makeAttachmentPointsToggle("indexKnuckle", new Vector2(-0.05F, -0.05F));
      makeAttachmentPointsToggle("indexMiddleJoint", new Vector2(-0.07F, -0.18F));
      makeAttachmentPointsToggle("indexDistalJoint", new Vector2(-0.085F, -0.29F));
      makeAttachmentPointsToggle("indexTip", new Vector2(-0.09F, -0.39F));

      makeAttachmentPointsToggle("middleKnuckle", new Vector2(0.07F, -0.06F));
      makeAttachmentPointsToggle("middleMiddleJoint", new Vector2(0.07F, -0.2F));
      makeAttachmentPointsToggle("middleDistalJoint", new Vector2(0.07F, -0.32F));
      makeAttachmentPointsToggle("middleTip", new Vector2(0.08F, -0.43F));

      makeAttachmentPointsToggle("ringKnuckle", new Vector2(0.185F, -0.03F));
      makeAttachmentPointsToggle("ringMiddleJoint", new Vector2(0.21F, -0.16F));
      makeAttachmentPointsToggle("ringDistalJoint", new Vector2(0.22F, -0.28F));
      makeAttachmentPointsToggle("ringTip", new Vector2(0.235F, -0.39F));

      makeAttachmentPointsToggle("pinkyKnuckle", new Vector2(0.285F, 0.03F));
      makeAttachmentPointsToggle("pinkyMiddleJoint", new Vector2(0.33F, -0.06F));
      makeAttachmentPointsToggle("pinkyDistalJoint", new Vector2(0.37F, -0.14F));
      makeAttachmentPointsToggle("pinkyTip", new Vector2(0.39F, -0.22F));

      EditorGUILayout.EndVertical();
    }

    private void makeAttachmentPointsToggle(string relPropertyName, Vector2 offCenterPosImgSpace) {
      var relPropValue = _attachmentPointsProperty.FindPropertyRelative(relPropertyName);
      relPropValue.boolValue = EditorGUI.Toggle(makeToggleRect(_handTexRect.center
                                                               + new Vector2(offCenterPosImgSpace.x * _handTexRect.width,
                                                                             offCenterPosImgSpace.y * _handTexRect.height)),                           relPropValue.boolValue);
    }

    private const float TOGGLE_SIZE = 10.0F;
    private Rect makeToggleRect(Vector2 centerPos) {
      return new Rect(centerPos.x - TOGGLE_SIZE / 2F, centerPos.y - TOGGLE_SIZE / 2F, TOGGLE_SIZE, TOGGLE_SIZE);
    }

  }

}
