/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.Interaction.Internal;

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
      specifyCustomDecorator("_interactionLayer", drawInteractionLayerDecorator);

      specifyCustomDecorator("_drawControllerRuntimeGizmos", drawControllerRuntimeGizmoDecorator);
      specifyCustomPostDecorator("_drawControllerRuntimeGizmos", drawPostControllerRuntimeGizmoDecorator);
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();
    }

    public override bool RequiresConstantRepaint() {
      return Application.isPlaying;
    }

    private void drawInteractionLayerDecorator(SerializedProperty property) {
      if (!Physics.GetIgnoreLayerCollision(target.interactionNoContactLayer.layerIndex,
                                           target.contactBoneLayer.layerIndex)) {
        EditorGUILayout.HelpBox("The No Contact layer should NOT collide with the Contact "
                              + "Bone layer. (Check your layer collision settings in Edit "
                              + "/Project Settings/Physics.)", MessageType.Error);
      }

      if (Physics.GetIgnoreLayerCollision(target.interactionLayer.layerIndex,
                                          target.contactBoneLayer.layerIndex)) {
        EditorGUILayout.HelpBox("The Interaction layer should collide with the Contact "
                              + "Bone layer. (Check your layer collision settings in Edit "
                              + "/Project Settings/Physics.)", MessageType.Error);
      }
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

    private void drawPostControllerRuntimeGizmoDecorator(SerializedProperty property) {
      if (property.boolValue && _runtimeGizmoManager != null) {
        drawControllerRuntimeGizmosColorLegend();
      }
    }

    public void drawControllerRuntimeGizmosColorLegend() {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Controller Gizmos Legend", EditorStyles.boldLabel);

      EditorGUI.BeginDisabledGroup(true);

      EditorGUILayout.ColorField(new GUIContent("Contact Bone Colliders",
                                                "The gizmo color for contact bone colliders "
                                              + "when soft contact is disabled."),
                                InteractionController.GizmoColors.ContactBone);
      EditorGUILayout.ColorField(new GUIContent("Soft Contact Bone Colliders",
                                                "The gizmo color for contact bones colliders "
                                              + "when soft contact is enabled."),
                                 InteractionController.GizmoColors.SoftContactBone);

      EditorGUILayout.Space();
      EditorGUILayout.ColorField(new GUIContent("Hover Points",
                                                "The gizmo color for hover points. Gizmo "
                                              + "does not reflect the actual hover radius."),
                                 InteractionController.GizmoColors.HoverPoint);
      EditorGUILayout.ColorField(new GUIContent("Primary Hover Points",
                                                "The gizmo color for primary hover points."),
                                 InteractionController.GizmoColors.PrimaryHoverPoint);

      EditorGUILayout.Space();
      EditorGUILayout.ColorField(new GUIContent("Grasp Points",
                                                "The gizmo color for grasp points. "
                                              + "InteractionHands do not use grasp points, "
                                              + "so no gizmo is drawn for them."),
                                 InteractionController.GizmoColors.GraspPoint);
      EditorGUILayout.ColorField(new GUIContent("Graspable Objects",
                                                "The gizmo color for the wire sphere "
                                              + "that appears at objects when they are "
                                              + "graspable by an interaction controller. "),
                                 InteractionController.GizmoColors.Graspable);

      EditorGUI.EndDisabledGroup();
    }

    private void drawControllersStatusEditor(SerializedProperty property) {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Interaction Controller Status", EditorStyles.boldLabel);

      if (target.interactionControllers.Count == 0) {
        EditorGUILayout.HelpBox("This Interaction Manager has no interaction controllers "
                              + "assigned to it. Please add at least one InteractionHand "
                              + "or an InteractionVRController as a child of this object.",
                              MessageType.Warning);

        return;
      }

      EditorGUILayout.BeginVertical();

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
      EditorGUILayout.ObjectField(controller, typeof(InteractionController), true);
      EditorGUI.EndDisabledGroup();

      // Status
      var messages = statusMessagesBuffer;
      messages.Clear();

      // Check various states and add messages to the messages buffer.
      checkTrackingStatus(controller, messages);

      if (controller.intHand != null) {
        checkInteractionHandStatus(controller.intHand, messages);
      }
      else if (controller is InteractionXRController) {
        checkInteractionVRControllerStatus(controller as InteractionXRController, messages);
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
      if (intHand.handDataMode != HandDataMode.Custom) {
        int index = target.interactionControllers.Query().IndexOf(intHand);

        if (target.interactionControllers.Query().
                                          Take(index).
                                          OfType<InteractionHand>().
                                          Where(h => h.handDataMode == intHand.handDataMode).
                                          Where(h => h.leapProvider == intHand.leapProvider).
                                          Any()) {
          messages.Add(new ControllerStatusMessage() {
            message = "Duplicate Hand",
            tooltip = "You already have a hand with this data mode in your scene. "
                    + "You should remove one of the duplicates.",
            color = Colors.Problem
          });
        }
      }
    }

    private InteractionXRController _leftVRNodeController;
    private InteractionXRController _rightVRNodeController;

    private void checkInteractionVRControllerStatus(InteractionXRController controller,
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
      bool isLeftVRNodeController  = controller.trackingProvider is DefaultXRNodeTrackingProvider
                                  && controller.chirality == Chirality.Left;
      bool isRightVRNodeController = controller.trackingProvider is DefaultXRNodeTrackingProvider
                                  && controller.chirality == Chirality.Right;

      if (isLeftVRNodeController && _leftVRNodeController != null
             || isRightVRNodeController && _rightVRNodeController != null) {

        var alreadyExistsController = isLeftVRNodeController ? _leftVRNodeController : _rightVRNodeController;

        string message;
        string tooltip;
        Color color;

        if (controller.deviceJoystickTokens.Equals(alreadyExistsController.deviceJoystickTokens)) {
          message = "Duplicate VR Controller";
          tooltip = "You already have a VRNode controller with this chirality and device "
                  + "string in your scene. You should remove one of the duplicates.";
          color = Colors.Problem;
        } else {
          message = "Multiple VR Controllers";
          tooltip = "You have multiple VR controllers of the same chirality enabled with "
                  + "different device strings. If both device strings match attached "
                  + "Unity joysticks, you may get duplicate controllers.";
          color = Colors.Caution;
        }

        messages.Add(new ControllerStatusMessage() {
          message = message,
          tooltip = tooltip,
          color = color
        });
      }

      if (isLeftVRNodeController) {
        _leftVRNodeController = controller;
      }
      if (isRightVRNodeController) {
        _rightVRNodeController = controller;
      }

      string wrongChiralityToken = controller.isLeft ? "right" : "left";
      if (controller.deviceJoystickTokens.Contains(wrongChiralityToken)) {
        messages.Add(new ControllerStatusMessage() {
          message = "Wrong Chirality?",
          tooltip = "This VR controller's device joystick string specifies a chirality "
                  + "that is different from the chirality of the controller itself. You "
                  + "should confirm this controller's device string or chirality setting.",
          color = Colors.Warning
        });
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
