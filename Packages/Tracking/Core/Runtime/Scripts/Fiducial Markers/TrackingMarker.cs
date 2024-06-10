/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{
    public class TrackingMarker : MonoBehaviour
    {
        [Tooltip("The AprilTag marker ID associated with this marker." +
            "\n\nNote: This must be unique within the scene")]
        public int id;

        [HideInInspector] public bool IsHighlighted = false;
        [HideInInspector] public bool IsTracked = false;
        [HideInInspector] public string DebugText = "";

        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<MeshRenderer>();
        }

        private void Update()
        {
            if (_renderer == null)
                return;

            _renderer.material.color = IsHighlighted ? Color.green : IsTracked ? new Color(1, 0.5f, 0) : Color.red;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Handles.Label(transform.position, DebugText);
        }
#endif
    }
}