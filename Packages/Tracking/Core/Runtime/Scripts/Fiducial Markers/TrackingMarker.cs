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

        public bool IsHighlighted = false;
        public bool IsTracked = false;

        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<MeshRenderer>();
        }

        private void Update()
        {
            _renderer.material.color = IsHighlighted ? Color.green : IsTracked ? new Color(1, 0.5f, 0) : Color.red;
        }
    }
}