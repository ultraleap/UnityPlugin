/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap
{

    [CustomEditor(typeof(CapsuleHand), editorForChildClasses: true), CanEditMultipleObjects]
    public class CapsuleHandEditor : CustomEditorBase<CapsuleHand>
    {
        bool showAdvanced = false;

        public override void OnInspectorGUI()
        {
            CapsuleHand targ = target;
            CapsuleHand.CapsuleHandPreset newValue = (CapsuleHand.CapsuleHandPreset)EditorGUILayout.EnumPopup("Preset", targ.Preset);

            if (newValue != targ.Preset)
            {
                targ.ChangePreset(newValue);
                EditorUtility.SetDirty(target);
            }

            DrawDefaultInspector();

            EditorGUILayout.Space();

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");

            if (showAdvanced)
            {
                var prevParent = targ.MeshParent;
                targ.MeshParent = (GameObject) EditorGUILayout.ObjectField("Mesh Parent", targ.MeshParent, typeof(GameObject), true);

                if (prevParent != targ.MeshParent)
                {
                    EditorUtility.SetDirty(targ);
                }

                EditorGUILayout.Space();

                GUI.enabled = targ.MeshParent != null;

                if (GUILayout.Button("Capture Mesh"))
                {
                    GameObject capturedMesh = target.CaptureMesh();
                    capturedMesh.transform.parent = targ.MeshParent.transform;
                }

                GUI.enabled = true;
            }
        }
    }
}