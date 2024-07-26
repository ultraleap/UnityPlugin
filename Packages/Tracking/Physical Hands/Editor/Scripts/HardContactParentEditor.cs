/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace Leap.PhysicalHands
{
    [CustomEditor(typeof(HardContactParent))]
    public class HardContactParentEditor : CustomEditorBase<HardContactParent>
    {
        private bool advancedFoldedOut = false;

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawScriptField((MonoBehaviour)target);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("physicalHandsManager"), new GUIContent("Physical Hands Manager", "The Physical Hands Manager associated with this Contact Parent. This will be automaticvally populated at runtime where possible."));

            EditorGUILayout.Space(5);

            advancedFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(advancedFoldedOut, "Advanced Options");

            if (advancedFoldedOut)
            {
                EditorGUILayout.HelpBox("Warning! Adjusting these values may cause unexpected results when using Hard Contact Physical Hands.", MessageType.Warning);

                EditorGUILayout.Space(5);

                EditorGUI.indentLevel = 1;

                EditorGUILayout.LabelField("Hand velocities", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPalmVelocity"), new GUIContent("Max Palm Velocity", "The maximum velocity that the Physical Hands palm will move towards the tracked position."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minFingerVelocity"), new GUIContent("Grabbed Finger Velocity", "The maximum velocity the fingers will move towards their tracked positions when grabbing to reduce physics forces."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxFingerVelocity"), new GUIContent("UnGrabbed Finger Velocity", "The maximum velocity the fingers will move towards their tracked positions when not grabbing."));

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Teleporting", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("teleportDistance"), new GUIContent("Teleport Distance", "The maximum distance the Physical Hands palm can be from the tracked position before it will telport to avoid unacceptable misalignment or extreme forces."));

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Bone Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boneMass"), new GUIContent("Bone Mass", "The mass of each finger bone."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boneStiffness"), new GUIContent("Bone Stiffness", "The stiffness of each bone's joint when not grabbing."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grabbedBoneStiffness"), new GUIContent("Grabbed Bone Stiffness", "The stiffness of each bone's joint when grabbing."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boneDamping"), new GUIContent("Bone Damping", "The damping applied to each bone's joint."));

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Physics Iterations", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useProjectPhysicsIterations"), new GUIContent("Use Project Physics Iterations", "Should the project's physics settings for solver iterations be used, or overridden for the Physical Hands specifically?"));

                if (!serializedObject.FindProperty("useProjectPhysicsIterations").boolValue)
                {
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("handSolverIterations"), new GUIContent("Hand Solver Iterations", ""));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("handSolverVelocityIterations"), new GUIContent("Hand Solver Velocity Iterations", ""));
                    EditorGUI.indentLevel = 1;
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Contact Distances", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactEnterDistance"), new GUIContent("Contact Enter Distance", "The maximum distance that a bone must be from a collider before it is considered to have started contacting."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactExitDistance"), new GUIContent("Contact Exit Distance", "The maximum distance that a contacting bone can be from a collider before it is considered to not be contacting any more."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactThumbEnterDistance"), new GUIContent("Contact Thumb Enter Distance", "The maximum distance that a thumb bone must be from a collider before it is considered contacting."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contactThumbExitDistance"), new GUIContent("Contact Thumb Exit Distance", "The maximum distance that a contacting thumb bone can be from a collider before it is considered to not be contacting any more."));

                EditorGUI.indentLevel = 0;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}