/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

namespace Leap.Unity.Examples
{
    /// <summary>
    /// This creates a circle and dashes with line renderers.
    /// It takes info such as the height and radius of the turntable from Turntable.
    /// </summary>
    [ExecuteInEditMode]
    public class TurntableVisuals : MonoBehaviour
    {
        [SerializeField] private LineRenderer _circle;
        [SerializeField, Tooltip("Offset of the dashes from the circle. Between 0 and 1")]
        private float _heightOffset = 0.01f;
        [SerializeField] private Mesh _dashMesh;
        private const float DASH_MESH_LENGTH = 0.15f; // length of the dash mesh in meters
        [SerializeField] private Material _dashMaterial;

        [SerializeField, Min(0)] private int _circleSegments = 36;

        private float _arcDegrees = 360f;
        public float ArcDegrees
        {
            set
            {
                _arcDegrees = value;
                if (_circlePositions == null || _circlePositions.Length != _circleSegments)
                {
                    UpdateVisuals();
                }
                else
                {
                    DrawTurntable();
                }
            }
            get
            {
                return _arcDegrees;
            }
        }

        private Vector3[] _circlePositions;
        private Matrix4x4[] _dashMatrices;

        private Turntable _turntable;

        private Camera _editTimeSceneCamera;

#if UNITY_EDITOR
        private Matrix4x4[] _rotationMatrices;
#endif

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UpdateVisuals();
            }
        }

        private void OnEnable()
        {
            _turntable = GetComponent<Turntable>();
        }

        [ExecuteInEditMode]
        private void Update()
        {
            if (_circlePositions == null || _circlePositions.Length != _circleSegments)
            {
                UpdateVisuals();
            }
            else
            {
                DrawTurntable();
            }
        }

        /// <summary>
        /// Updates the circle and dashes visual of the turntable
        /// </summary>
        public void UpdateVisuals()
        {
            if (_turntable == null) return;

            float height = _turntable.TableHeight;
            float lowerHeight = _turntable.LowerLevelHeight;
            float radius = _turntable.TableRadius;
            float lowerRadius = _turntable.LowerLevelRadius;

            Vector3 dashStartPos = new Vector3(0f, height, radius);
            Vector3 dashEndPos = new Vector3(0f, lowerHeight, lowerRadius);

            Vector3 dashPos = dashStartPos + _heightOffset * (dashEndPos - dashStartPos);
            Vector3 dashDirection = dashStartPos - dashEndPos;
            Quaternion dashRot = Quaternion.FromToRotation(new Vector3(0, 0, 1), dashDirection);
            Vector3 DashScale = Vector3.one * dashDirection.magnitude / DASH_MESH_LENGTH * (1f - _heightOffset);

            Matrix4x4 dashMatrix = Matrix4x4.TRS(dashPos, dashRot, DashScale);

            _dashMatrices = new Matrix4x4[_circleSegments];
            _circlePositions = new Vector3[_circleSegments];

            float x;
            float y = height;
            float z;

            float angle = 0f;

            for (int i = 0; i < _circleSegments; i++)
            {
                x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

                _circlePositions[i] = new Vector3(x, y, z);

                _dashMatrices[i] = Matrix4x4.Rotate(Quaternion.Euler(0, angle, 0)) * dashMatrix;

                angle += (360f / _circleSegments);
            }

            DrawTurntable();
        }

        private void DrawTurntable()
        {
            if (_dashMesh == null || _dashMaterial == null || _dashMatrices == null) return;

            _circle.positionCount = (int)(_circleSegments * _arcDegrees / 360f);
            _circle.useWorldSpace = false;
            if (_arcDegrees == 360f) _circle.loop = true;
            else _circle.loop = false;

            for (int i = 0; i < _circle.positionCount; i++)
            {
                _circle.SetPosition(i, _circlePositions[i]);
            }

            // apply current transform to matrices
            Matrix4x4[] rotatedMatrices = new Matrix4x4[_circle.positionCount];
            Matrix4x4 currentTransform = transform.localToWorldMatrix;
            for (int i = 0; i < _circle.positionCount; i++)
            {
                rotatedMatrices[i] = currentTransform * _dashMatrices[i];
            }

            if (Application.isPlaying)
            {
                Graphics.DrawMeshInstanced(_dashMesh, 0, _dashMaterial, rotatedMatrices, _circle.positionCount, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
            }
#if UNITY_EDITOR            
            else
            {
                _rotationMatrices = rotatedMatrices;
            }
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Vector3 pos, scale;
            Quaternion rot;

            for (int i = 0; i < _circle.positionCount; i++)
            {
                DecomposeTRSMatrix(_rotationMatrices[i], out pos, out rot, out scale);
                Gizmos.DrawMesh(_dashMesh, pos, rot, scale);
            }
        }

        private void DecomposeTRSMatrix(Matrix4x4 m, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            // Extract new local position
            translation = m.GetColumn(3);

            // Extract new local rotation
            rotation = Quaternion.LookRotation(
                m.GetColumn(2),
                m.GetColumn(1)
            );

            // Extract new local scale
            scale = new Vector3(
                m.GetColumn(0).magnitude,
                m.GetColumn(1).magnitude,
                m.GetColumn(2).magnitude
            );
        }
#endif
    }
}