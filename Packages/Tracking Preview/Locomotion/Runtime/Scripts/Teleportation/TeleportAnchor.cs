/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Preview.HandRays;
using System;
using UnityEngine;

namespace Leap.Preview.Locomotion
{
    /// <summary>
    /// A teleport anchor is a point in space a user is able to teleport to
    /// </summary>
    public class TeleportAnchor : FarFieldObject
    {
        [SerializeField, Tooltip("The main teleport anchor mesh.")]
        private MeshRenderer _markerMesh = null;

        [SerializeField, Tooltip("The gameobject containing the objects which indicate which way the user will face")]
        private Transform _rotationIndicators = null;

        [SerializeField, ColorUsage(true, true)]
        private Color _idleColor = new Color(1, 1, 1, 0.25f);

        [SerializeField, ColorUsage(true, true)]
        private Color32 _highlightedColor = new Color(1, 1, 1, 0.25f);

        [SerializeField, Tooltip("A higher value will tile the texture more and appear smaller.")]
        private float _idleSize = 4f, _highlightedSize = 1f;

        [SerializeField, Tooltip("The speed at which the markers visuals will transition.")]
        private float _transitionTime = 0.2f;

        private float _oldTransition = 0f;
        private float _currentTransition = 0f;

        private bool _isHighlighted = false;

        private Material _storedMaterial;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Quaternion initialRotationIndicatorsRotation;

        public Action<TeleportAnchor> OnTeleportedTo;

        private void Awake()
        {
            UpdateInitialPositions();

            Material material = _markerMesh.sharedMaterial;
            if (material != null)
            {
                _storedMaterial = new Material(material);
                _markerMesh.sharedMaterial = _storedMaterial;
                UpdateMaterials();
            }
        }

        public virtual void Update()
        {
            UpdateVisuals();
        }

        public virtual void OnDisable()
        {
            ResetPoint();
        }

        /// <summary>
        /// Updates the saved initial position and rotation values using the position and rotations provided as arguments.
        /// </summary>
        /// <param name="pos">Provide a position that the saved initial position should be</param>
        /// <param name="rot">Provide a rotation that the saved initial rotation should be</param>
        /// <param name="rotationIndicatorRot">Provide a rotation that the saved initial indicator rotation should be</param>
        public void UpdateInitialPositions(Vector3 pos, Quaternion rot, Quaternion rotationIndicatorRot)
        {
            initialPosition = pos;
            initialRotation = rot;
            initialRotationIndicatorsRotation = rotationIndicatorRot;
        }

        /// <summary>
        /// Updates the saved initial position and rotation values using the current transform of the tp anchor
        /// </summary>
        public void UpdateInitialPositions()
        {
            UpdateInitialPositions(transform.position, transform.rotation, _rotationIndicators.rotation);
        }

        /// <summary>
        /// Sets the teleport anchor as highlighted, or unhighlighted
        /// </summary>
        /// <param name="highlighted">Whether the teleport anchor is highlighted or unhighlighted</param>
        public void SetHighlighted(bool highlighted = true)
        {
            _isHighlighted = highlighted;

            if (!_isHighlighted)
            {
                ResetPoint();
            }
        }

        /// <summary>
        /// Points the rotation visuals to a new rotation
        /// </summary>
        /// <param name="newRotation">The rotation to point the rotation visuals to</param>
        public void UpdateRotationVisuals(Quaternion newRotation)
        {
            _rotationIndicators.rotation = newRotation;
        }

        private void UpdateVisuals()
        {
            _currentTransition += Time.deltaTime * ((_isHighlighted ? 1f : -1f) / _transitionTime);
            if (_isHighlighted)
            {
                if (_currentTransition > 0.9999f)
                {
                    _currentTransition = 1f;
                }
            }
            else
            {
                if (_currentTransition < 1e-4)
                {
                    _currentTransition = 0f;
                }
            }
            if (_currentTransition != _oldTransition)
            {
                UpdateMaterials();
            }
            _oldTransition = _currentTransition;
        }

        private void UpdateMaterials()
        {
            _storedMaterial.SetColor("_MainColor", Color.Lerp(_idleColor, _highlightedColor, _currentTransition));
            _storedMaterial.mainTextureScale = new Vector2(1, Mathf.Lerp(_idleSize, _highlightedSize, _currentTransition));
        }

        protected virtual void ResetPoint()
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            _rotationIndicators.rotation = initialRotationIndicatorsRotation;
        }
    }
}