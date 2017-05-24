/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionManager))]
  public class InteractionManagerEditor : CustomEditorBase<InteractionManager> {

    protected override void OnEnable() {
      base.OnEnable();

      // Interaction Controllers
      specifyCustomDrawer("_interactionControllers", drawControllersStatusEditor);

      // Layers
      SerializedProperty autoGenerateLayerProperty = serializedObject.FindProperty("_autoGenerateLayers");
      specifyConditionalDrawing(() => autoGenerateLayerProperty.boolValue,
                                "_templateLayer");
      specifyConditionalDrawing(() => !autoGenerateLayerProperty.boolValue,
                                "_interactionLayer",
                                "_interactionNoContactLayer",
                                "_contactBoneLayer");

      specifyCustomDecorator("_drawControllerRuntimeGizmos", drawControllerRuntimeGizmoDecorator);
    }

    public override bool RequiresConstantRepaint() {
      return Application.isPlaying;
    }

    private RuntimeGizmoManager _runtimeGizmoManager;

    private void drawControllerRuntimeGizmoDecorator(SerializedProperty property) {
      if (property.boolValue && _runtimeGizmoManager == null) {
        _runtimeGizmoManager = FindObjectOfType<RuntimeGizmoManager>();

        if (_runtimeGizmoManager == null) {
          EditorGUILayout.Space();
          EditorGUILayout.Space();
          EditorGUILayout.HelpBox("Draw Controller Runtime Gizmos is checked, but there "
                                + "is no RuntimeGizmoManager in your scene, or it is "
                                + "disabled.", MessageType.Warning);
        }
      }
    }

    private void drawControllersStatusEditor(SerializedProperty property) {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Interaction Controller Status", EditorStyles.boldLabel);

      EditorGUILayout.BeginVertical();

      _leftHand = null;
      _rightHand = null;
      _leftVRNodeController = null;
      _rightVRNodeController = null;
      foreach (var controller in target.interactionControllers) {
        EditorGUILayout.BeginHorizontal();

        drawControllerStatusEditor(controller);

        EditorGUILayout.EndHorizontal();
      }

      EditorGUILayout.EndVertical();
    }

    public static class Colors {
      public static Color DarkGray  { get { return new Color(0.4F, 0.4F, 0.4F); } }
      public static Color LightGray { get { return new Color(0.7F, 0.7F, 0.7F); } }

      public static Color Good      { get { return Color.Lerp(Color.green, LightGray, 0.2F); } }
      public static Color Caution   { get { return Color.Lerp(Good, Color.yellow, 0.8F); } }
      public static Color Warning   { get { return Color.Lerp(Color.yellow, Problem, 0.5F); } }
      public static Color Problem   { get { return Color.Lerp(Color.red, Color.yellow, 0.3F); } }
    }

    private struct ControllerStatusMessage {
      public string message;
      public string tooltip;
      public Color color;
    }
    private List<ControllerStatusMessage> statusMessagesBuffer = new List<ControllerStatusMessage>();

    private void drawControllerStatusEditor(InteractionController controller) {
      // Controller object
      EditorGUI.BeginDisabledGroup(true);
      EditorGUILayout.ObjectField(controller, typeof(InteractionController), true,
                                  GUILayout.MinHeight(20));
      EditorGUI.EndDisabledGroup();

      // Status
      var messages = statusMessagesBuffer;
      messages.Clear();

      // Check various states and add messages to the messages buffer.
      checkTrackingStatus(controller, messages);

      if (controller.intHand != null) {
        checkInteractionHandStatus(controller.intHand, messages);
      }
      else if (controller is InteractionVRController) {
        checkInteractionVRControllerStatus(controller as InteractionVRController, messages);
      }

      // Render the status messages.
      Rect statusMessagesRect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(20));

      EditorGUI.DrawRect(statusMessagesRect, Colors.DarkGray);
      statusMessagesRect = statusMessagesRect.ShrinkOne();
      EditorGUI.DrawRect(statusMessagesRect, Colors.LightGray);
      statusMessagesRect = statusMessagesRect.ShrinkOne();
      EditorGUI.DrawRect(statusMessagesRect, Colors.DarkGray);

      if (messages.Count == 0) {
        messages.Add(new ControllerStatusMessage() {
          message = "No Status Messages",
          tooltip = "",
          color = Colors.Good
        });
      }

      foreach (var statusMessage in messages) {
        var messageColorStyle = new GUIStyle(EditorStyles.label);
        messageColorStyle.normal.textColor = statusMessage.color;

        EditorGUILayout.LabelField(new GUIContent("[" + statusMessage.message + "]",
                                                  statusMessage.tooltip),
                                   messageColorStyle);
        GUILayout.Space(1);
      }
      GUILayout.Space(1);

      EditorGUILayout.EndVertical();
    }

    private void checkTrackingStatus(InteractionController controller,
                                     List<ControllerStatusMessage> messages) {
      if (Application.isPlaying) {
        if (controller.isTracked) {
          if (controller.isBeingMoved) {
            messages.Add(new ControllerStatusMessage() {
              message = "Tracked",
              tooltip = "This interaction controller is currently being tracked.",
              color = Colors.Good
            });
          } else {
            messages.Add(new ControllerStatusMessage() {
              message = "Not Moving",
              tooltip = "This interaction controller is currently not being moved.",
              color = Colors.Caution
            });
          }
        } else {
          messages.Add(new ControllerStatusMessage() {
            message = "Untracked",
            tooltip = "This interaction controller is not currently being tracked.",
            color = Colors.Warning
          });
        }
      }
    }

    private LeapProvider _provider = null;
    private InteractionHand _leftHand = null;
    private InteractionHand _rightHand = null;

    private void checkInteractionHandStatus(InteractionHand intHand,
                                            List<ControllerStatusMessage> messages) {
      if (!Application.isPlaying) {
        // Check for valid InteractionHand data state.
        if (intHand.handDataMode == HandDataMode.Custom) {
          messages.Add(new ControllerStatusMessage() {
            message = "HandDataMode: Custom",
            tooltip = "This interaction hand has its data mode set to Custom. "
                    + "A custom script will be required to ensure hand data gets to "
                    + "the interaction hand properly. Upon pressing play, an error will "
                    + "be raised by the hand itself if it is misconfigured.",
            color = Colors.Caution
          });
        }
        else {
          // Check for a LeapProvider in the scene somewhere.
          if (_provider == null) {
            _provider = FindObjectOfType<LeapProvider>();
          }
          if (_provider == null) {
            messages.Add(new ControllerStatusMessage() {
              message = "No LeapProvider",
              tooltip = "No LeapProvider object was found in your scene. "
                      + "InteractionHands require a LeapProvider to function; consider "
                      + "dragging in the LeapHeadMountedRig prefab or creating and "
                      + "configuring a LeapServiceProvider.",
              color = Colors.Warning
            });
          }
        }
      }

      // Check if the player has multiple left hands or multiple right hands.
      if (intHand.handDataMode == HandDataMode.PlayerLeft && _leftHand != null
       || intHand.handDataMode == HandDataMode.PlayerRight && _rightHand != null) {
        messages.Add(new ControllerStatusMessage() {
          message = "Duplicate Hand",
          tooltip = "You already have a hand with this data mode in your scene. "
                  + "You should remove one of the duplicates.",
          color = Colors.Problem
        });
      }
      if (_leftHand == null && intHand.handDataMode == HandDataMode.PlayerLeft) {
        _leftHand = intHand;
      }
      else if (_rightHand == null && intHand.handDataMode == HandDataMode.PlayerRight) {
        _rightHand = intHand;
      }
    }

    private InteractionVRController _leftVRNodeController;
    private InteractionVRController _rightVRNodeController;

    private void checkInteractionVRControllerStatus(InteractionVRController controller,
                                                    List<ControllerStatusMessage> messages) {
      // Check if the controller is configured correctly if it is set up with a custom
      // tracking provider.
      if (controller.isUsingCustomTracking) {
        messages.Add(new ControllerStatusMessage() {
          message = "Custom Tracking Provider",
          tooltip = "You are using a custom tracking provider for this VR controller.",
          color = Colors.Caution
        });
      }

      // Check if the player has duplicate VRNode left controllers or right controllers.
      bool isLeftVRNodeController  = controller.trackingProvider is DefaultVRNodeTrackingProvider
                                  && controller.chirality == Chirality.Left;
      bool isRightVRNodeController = controller.trackingProvider is DefaultVRNodeTrackingProvider
                                  && controller.chirality == Chirality.Right;

      if (isLeftVRNodeController  &&  _leftVRNodeController != null
       || isRightVRNodeController && _rightVRNodeController != null) {
        messages.Add(new ControllerStatusMessage() {
          message = "Duplicate VR Controller",
          tooltip = "You already have a VRNode controller with this chirality in your "
                  + "scene. You should remove one of the duplicates.",
          color = Colors.Problem
        });
      }
      if (isLeftVRNodeController) {
        _leftVRNodeController = controller;
      }
      if (isRightVRNodeController) {
        _rightVRNodeController = controller;
      }
    }

  }

  public static class Extensions {

    public static Rect ShrinkOne(this Rect rect) {
      rect.x += 1;
      rect.y += 1;
      rect.width -= 2;
      rect.height -= 2;
      return rect;
    }

  }

}
