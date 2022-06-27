/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Rendering;

namespace Leap.Unity
{
    using Attributes;

    /// <summary>
    /// Moves the camera to each eye position on pre-render. Only necessary for image
    /// pass-through (IR viewer) scenarios.
    /// </summary>
    public class LeapEyeDislocator : MonoBehaviour
    {

        [SerializeField]
        private bool _useCustomBaseline = false;

        [MinValue(0), Units("MM"), InspectorName("Baseline")]
        [SerializeField]
        private float _customBaselineValue = 64;

        [SerializeField]
        private bool _showEyePositions = false;

        [SerializeField] private LeapServiceProvider _provider = null;
        private float _deviceBaseline = -1;
        private bool _hasVisitedPreCull = false;

        [SerializeField] private Camera _camera = null;

        private void onDevice(Device device)
        {
            if (device == _provider.CurrentDevice || _deviceBaseline == -1)
                _deviceBaseline = device.Baseline;
        }

        private void OnDestroy()
        {
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                RenderPipelineManager.beginCameraRendering -= onBeginRendering;
            }
            else
            {
                Camera.onPreCull -= OnCameraPreCull; // No multiple-subscription.
            }
        }

        private void OnEnable()
        {
            if (_provider == null)
            {
                enabled = false;
                return;
            }

            if (GraphicsSettings.renderPipelineAsset != null)
            {
                RenderPipelineManager.beginCameraRendering -= onBeginRendering;
                RenderPipelineManager.beginCameraRendering += onBeginRendering;
            }
            else
            {
                Camera.onPreCull -= OnCameraPreCull; // No multiple-subscription.
                Camera.onPreCull += OnCameraPreCull;
            }

            _provider.OnDeviceSafe += onDevice;
        }

        private void OnDisable()
        {
            if (_camera == null) return;

            _camera.ResetStereoViewMatrices();

            Camera.onPreCull -= OnCameraPreCull;

            _provider.OnDeviceSafe -= onDevice;
        }

        private void Update()
        {
            if (_camera == null) return;

#if !UNITY_2020_1_OR_NEWER
            _camera.ResetStereoViewMatrices();
#endif

            _hasVisitedPreCull = false;
        }

        protected virtual void onBeginRendering(ScriptableRenderContext context, Camera camera) { OnCameraPreCull(camera); }

        private void OnCameraPreCull(Camera cam)
        {
            if (_hasVisitedPreCull || cam != _camera)
            {
                return;
            }
            _hasVisitedPreCull = true;

#if UNITY_2020_1_OR_NEWER
            // Unity rendering system changed in 2020. Performing this in update causes misalignment.
            // XR applications need to be rendered in multipass or it will fail.
            _camera.ResetStereoViewMatrices();
#endif

            float baselineToUse = -1;
            if (_useCustomBaseline)
            {
                baselineToUse = _customBaselineValue;
            }
            else
            {
                if (_deviceBaseline == -1)
                {
                    _provider.OnDeviceSafe -= onDevice;
                    _provider.OnDeviceSafe += onDevice;
                }
                baselineToUse = _deviceBaseline;
            }

            float baselineValue;
            if (baselineToUse != -1)
            {
                baselineValue = baselineToUse;

                baselineValue *= 1e-3f;

                Matrix4x4 leftMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                Matrix4x4 rightMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

                Vector3 leftPos = leftMat.inverse.MultiplyPoint3x4(Vector3.zero);
                Vector3 rightPos = rightMat.inverse.MultiplyPoint3x4(Vector3.zero);
                float existingBaseline = Vector3.Distance(leftPos, rightPos);

                float baselineAdjust = baselineValue - existingBaseline;

                adjustViewMatrix(Camera.StereoscopicEye.Left, baselineAdjust);
                adjustViewMatrix(Camera.StereoscopicEye.Right, baselineAdjust);
            }
        }

        private void adjustViewMatrix(Camera.StereoscopicEye eye, float baselineAdjust)
        {
            float eyeOffset = eye == Camera.StereoscopicEye.Left ? 1 : -1;
            Vector3 ipdOffset = eyeOffset * Vector3.right * baselineAdjust * 0.5f;
            Vector3 providerForwardOffset = Vector3.zero,
                    providerVerticalOffset = Vector3.zero;
            Quaternion providerRotation = Quaternion.Euler(0f, 180f, 0f);
            if (_provider is LeapXRServiceProvider || _provider.GetType().BaseType == typeof(LeapXRServiceProvider))
            {
                LeapXRServiceProvider _xrProvider = _provider as LeapXRServiceProvider;
                providerForwardOffset = Vector3.forward * _xrProvider.deviceOffsetZAxis;
                providerVerticalOffset = -Vector3.up * _xrProvider.deviceOffsetYAxis;
                providerRotation = Quaternion.AngleAxis(_xrProvider.deviceTiltXAxis, Vector3.right);
            }
            else
            {
                Matrix4x4 imageMatWarp = _camera.projectionMatrix
                                           * Matrix4x4.TRS(Vector3.zero, providerRotation, Vector3.one)
                                           * _camera.projectionMatrix.inverse;
                Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", imageMatWarp);
            }

            var existingMatrix = _camera.GetStereoViewMatrix(eye);
            _camera.SetStereoViewMatrix(eye, Matrix4x4.TRS(Vector3.zero, providerRotation, Vector3.one) *
                                             Matrix4x4.Translate(providerForwardOffset + ipdOffset) *
                                             Matrix4x4.Translate(providerVerticalOffset) *
                                             existingMatrix);
        }

        private void OnDrawGizmos()
        {
            if (_showEyePositions && Application.isPlaying)
            {
                Matrix4x4 leftMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                Matrix4x4 rightMat = _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

                Vector3 leftPos = leftMat.inverse.MultiplyPoint3x4(Vector3.zero);
                Vector3 rightPos = rightMat.inverse.MultiplyPoint3x4(Vector3.zero);

                Gizmos.color = Color.white;
                Gizmos.DrawSphere(leftPos, 0.02f);
                Gizmos.DrawSphere(rightPos, 0.02f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(leftPos, rightPos);
            }
        }
    }
}