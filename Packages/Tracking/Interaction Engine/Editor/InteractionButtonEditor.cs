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

    [CanEditMultipleObjects]
    [CustomEditor(typeof(InteractionButton), editorForChildClasses: true)]
    public class InteractionButtonEditor : InteractionBehaviourEditor
    {

        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing(() => false, "graspedMovementType");
        }

        public override void OnInspectorGUI()
        {
            InteractionButton button = target as InteractionButton;

            bool nonzeroRotation = button.transform.localRotation != Quaternion.identity;
            bool isRoot = button.transform == button.transform.root;
            var isPrefabAsset = Leap.Unity.Utils.IsObjectPartOfPrefabAsset(button.gameObject);

            EditorGUILayout.BeginHorizontal();
            if ((nonzeroRotation || isRoot) && !isPrefabAsset)
            {
                if (isRoot)
                {
                    EditorGUILayout.HelpBox("This button has no parent!  Buttons do not work without a parent transform.", MessageType.Warning);
                }
                else if (nonzeroRotation)
                {
                    EditorGUILayout.HelpBox("It looks like this button's local rotation is non-zero; would you like to add a parent transform so it depresses along its z-axis?", MessageType.Warning);
                }

                if (GUILayout.Button("Add Button\nParent Transform"))
                {
                    GameObject buttonBaseTransform = new GameObject(button.gameObject.name + " Base");
                    Undo.RegisterCreatedObjectUndo(buttonBaseTransform, "Created Button Base for " + button.gameObject.name);
                    Undo.SetTransformParent(buttonBaseTransform.transform, button.transform.parent, "Child " + button.gameObject.name + "'s Base to " + button.gameObject.name + "'s Parent");

                    Undo.RecordObject(buttonBaseTransform, "Set " + target.gameObject.name + "'s Base's Transform's Properties");
                    buttonBaseTransform.transform.localPosition = button.transform.localPosition;
                    buttonBaseTransform.transform.localRotation = button.transform.localRotation;
                    buttonBaseTransform.transform.localScale = button.transform.localScale;

                    Undo.SetTransformParent(button.transform, buttonBaseTransform.transform, "Child " + button.gameObject.name + " to its Base");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (!isRoot)
            {
                bool isUniform = (button.transform.parent.lossyScale.x.NearlyEquals(button.transform.parent.lossyScale.y) &&
                                  button.transform.parent.lossyScale.y.NearlyEquals(button.transform.parent.lossyScale.z) &&
                                  button.transform.parent.lossyScale.x.NearlyEquals(button.transform.parent.lossyScale.z));
                if (!isUniform)
                {
                    EditorGUILayout.HelpBox("This button exists within a non-uniformly scaled space!  Please check the parent transforms for non-uniform scale...", MessageType.Warning);
                }
            }

            EditorGUILayout.EndHorizontal();

            Rigidbody currentBody = button.GetComponent<Rigidbody>();
            RigidbodyConstraints constraints = currentBody.constraints;

            EditorGUILayout.BeginHorizontal();
            if (constraints != RigidbodyConstraints.FreezeRotation)
            {
                EditorGUILayout.HelpBox("It looks like this button can freely rotate around one or more axes; would you like to constrain its rotation?", MessageType.Warning);
                if (GUILayout.Button("Freeze\nRotation"))
                {
                    Undo.RecordObject(currentBody, "Set " + target.gameObject.name + "'s Rigidbody's Rotation Constraints to be frozen");
                    currentBody.constraints = RigidbodyConstraints.FreezeRotation;
                }
            }
            EditorGUILayout.EndHorizontal();

            base.OnInspectorGUI();
        }
    }
}