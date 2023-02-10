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
    [CustomEditor(typeof(InteractionBehaviour), editorForChildClasses: true)]
    public class InteractionBehaviourEditor : CustomEditorBase<InteractionBehaviour>
    {

        private EnumEventTableEditor _tableEditor;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Interaction Manager hookup.
            specifyCustomDecorator("_manager", drawInteractionManagerDecorator);

            deferProperty("_eventTable");
            specifyCustomDrawer("_eventTable", drawEventTable);

            specifyConditionalDrawing(() => !targets.All(intObj => intObj.ignoreContact),
                                      "_contactForceMode");

            specifyConditionalDrawing(() => !targets.All(intObj => intObj.ignoreGrasping),
                                      "_allowMultiGrasp",
                                      "_moveObjectWhenGrasped",
                                      "graspedMovementType");

            // Layer Overrides
            specifyConditionalDrawing(() => targets.Any(intObj => intObj.overrideInteractionLayer),
                                      "_interactionLayer");
            specifyCustomDecorator("_interactionLayer", drawInteractionLayerDecorator);
            specifyConditionalDrawing(() => targets.Any(intObj => intObj.overrideNoContactLayer),
                                      "_noContactLayer");
            specifyCustomDecorator("_noContactLayer", drawNoContactLayerDecorator);
        }

        private void drawInteractionManagerDecorator(SerializedProperty property)
        {
            bool shouldDrawInteractionManagerNotSetWarning = false;
            foreach (var target in targets)
            {
                if (Utils.IsObjectPartOfPrefabAsset(target)) continue;

                if (target.manager == null)
                {
                    shouldDrawInteractionManagerNotSetWarning = true;
                    break;
                }
            }
            if (shouldDrawInteractionManagerNotSetWarning)
            {
                bool pluralTargets = targets.Length > 1;
                string noManagerSetWarningMessage = "";
                if (pluralTargets)
                {
                    noManagerSetWarningMessage = "One or more of the currently selected interaction "
                                               + "objects doesn't have its Interaction Manager set. ";
                }
                else
                {
                    noManagerSetWarningMessage = "The currently selected interaction object doesn't "
                                               + "have its Interaction Manager set. ";
                }
                noManagerSetWarningMessage += " Object validation requires a configured manager "
                                            + "property.";

                drawSetManagerWarningBox(noManagerSetWarningMessage, MessageType.Error);
            }
        }

        private void drawSetManagerWarningBox(string warningMessage, MessageType messageType)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.HelpBox(warningMessage, messageType);

            Rect buttonRect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(100F),
                                                             GUILayout.ExpandHeight(true),
                                                             GUILayout.MaxHeight(40F));
            InteractionManager manager = InteractionManager.instance;
            EditorGUI.BeginDisabledGroup(manager == null);
            if (GUI.Button(buttonRect, new GUIContent("Auto-Fix",
                                        manager == null ? "Please add an Interaction Manager to "
                                                        + "your scene."
                                                    : "Use InteractionManager.instance to "
                                                    + "automatically set the manager of the "
                                                    + "selected interaction objects.")))
            {
                if (manager == null)
                {
                    Debug.LogError("Attempt to find an InteractionManager instance failed. Is there "
                                 + "an InteractionManager in your scene?");
                }
                else
                {
                    foreach (var target in targets)
                    {
                        if (Utils.IsObjectPartOfPrefabAsset(target)) continue;

                        Undo.RecordObject(target, "Auto-set Interaction Manager");
                        target.manager = manager;
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void drawInteractionLayerDecorator(SerializedProperty property)
        {
            bool shouldDrawWarning = false;
            foreach (var target in targets)
            {
                if (target.manager == null) continue; // Can't check.

                if (target.overrideInteractionLayer
                    && !target.manager.autoGenerateLayers
                    && Physics.GetIgnoreLayerCollision(target.interactionLayer.layerIndex,
                                                       target.manager.contactBoneLayer.layerIndex))
                {

                    shouldDrawWarning = true;
                    break;
                }
            }

            if (shouldDrawWarning)
            {
                bool pluralTargets = targets.Length > 1;
                string message;
                if (pluralTargets)
                {
                    message = "One or more of the selected interaction objects has its "
                            + "Interaction layer set NOT to collide with the contact bone "
                            + "layer. ";
                }
                else
                {
                    message = "The selected interaction object has its Interaction layer set "
                            + "NOT to collide with the contact bone layer. ";
                }
                message += "This will prevent an interaction object from supporting contact with "
                         + "any hands or controllers.";

                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }

        private void drawNoContactLayerDecorator(SerializedProperty property)
        {
            bool shouldDrawCollisionWarning = false;
            foreach (var target in targets)
            {
                if (target.manager == null) continue; // Can't check.

                if (target.overrideNoContactLayer
                    && !target.manager.autoGenerateLayers
                    && !Physics.GetIgnoreLayerCollision(target.noContactLayer.layerIndex,
                                                        target.manager.contactBoneLayer.layerIndex))
                {
                    shouldDrawCollisionWarning = true;
                    break;
                }
            }

            if (shouldDrawCollisionWarning)
            {
                bool pluralTargets = targets.Length > 1;
                string noContactErrorMessage;
                if (pluralTargets)
                {
                    noContactErrorMessage = "One or more selected interaction objects has its No "
                                            + "Contact layer set to collide with the contact bone "
                                            + "layer. ";
                }
                else
                {
                    noContactErrorMessage = "This interaction object has its No Contact layer set "
                                            + "to collide with the contact bone layer. ";
                }

                noContactErrorMessage += "Please ensure the Interaction Manager's contact bone "
                                         + "layer is set not to collide with any interaction "
                                         + "object's No Contact layer.";

                EditorGUILayout.HelpBox(noContactErrorMessage, MessageType.Error);
            }
        }

        public override void OnInspectorGUI()
        {
            checkHasColliders();

            base.OnInspectorGUI();
        }

        private void checkHasColliders()
        {
            bool anyMissingColliders = false;
            foreach (var singleTarget in targets)
            {
                if (singleTarget.GetComponentsInChildren<Collider>().Length == 0)
                {
                    anyMissingColliders = true; break;
                }
            }

            if (anyMissingColliders)
            {
                bool pluralObjects = targets.Length > 1;

                string message;
                if (pluralObjects)
                {
                    message = "One or more of the currently selected interaction objects have no "
                            + "colliders. Interaction objects without any Colliders cannot be "
                            + "interacted with.";
                }
                else
                {
                    message = "This interaction object has no Colliders. Interaction objects "
                            + "without any Colliders cannot be interacted with.";
                }

                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }

        private void drawEventTable(SerializedProperty property)
        {
            if (_tableEditor == null)
            {
                _tableEditor = new EnumEventTableEditor(property, typeof(InteractionBehaviour.EventType));
            }

            _tableEditor.DoGuiLayout();
        }
    }
}