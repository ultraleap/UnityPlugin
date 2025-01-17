/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;

using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// This script can be used instead of the LeapEyeDislocator when displaying an infrared image in the scene.
/// You should use this in cases where the EyeDislocator doesn't work (eg. Scriptable Render Pipelines, Single Pass Instanced Render Mode),
/// or in cases where you want to use it for any background tasks while leaving the scene camera's view matrix unchanged.
/// </summary>
public class LeapDistortImage : MonoBehaviour
{
    /// <summary>
    /// When using custom values, you can specify translation, rotation, and scale of the image
    /// </summary>
    public bool customValues = false;

    /// <summary>
    /// translation used when customValues = true
    /// </summary>
    public Vector3 translation = Vector3.zero;
    /// <summary>
    /// rotation used when customValues = true
    /// </summary>
    public Quaternion rotation = Quaternion.identity;
    /// <summary>
    /// scale used when customValues = true
    /// </summary>
    public Vector3 scale = Vector3.one;

    /// <summary>
    /// The camera in the scene
    /// </summary>
    public Camera _camera;
    /// <summary>
    /// The leap provider that is providing the images
    /// </summary>
    public LeapProvider _provider;
    /// <summary>
    /// The Renderer that renders the image which should have a material using the PassthroughBackground shader
    /// </summary>
    public Renderer ImageRenderer;

    // Start is called before the first frame update
    void OnEnable()
    {
#if UNITY_6000_0_OR_NEWER
        if (GraphicsSettings.defaultRenderPipeline != null)
#else
        if (GraphicsSettings.renderPipelineAsset != null)
#endif 
        {
            RenderPipelineManager.beginCameraRendering -= onBeginRendering;
            RenderPipelineManager.beginCameraRendering += onBeginRendering;
        }
        else
        {
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPreCull += OnCameraPreCull;
        }
    }

    private void OnDisable()
    {
#if UNITY_6000_0_OR_NEWER
        if (GraphicsSettings.defaultRenderPipeline != null)
#else
        if (GraphicsSettings.renderPipelineAsset != null)
#endif 
        {
            RenderPipelineManager.beginCameraRendering -= onBeginRendering;
        }
        else
        {
            Camera.onPreCull -= OnCameraPreCull;
        }
    }

    protected virtual void onBeginRendering(ScriptableRenderContext context, Camera camera) { OnCameraPreCull(camera); }

    // Update is called once per frame
    void OnCameraPreCull(Camera cam)
    {
        float deviceOffsetY = 0;
        float deviceOffsetZ = 0;
        float deviceTiltX = 0;

        if (_provider is LeapXRServiceProvider || _provider.GetType().BaseType == typeof(LeapXRServiceProvider))
        {
            LeapXRServiceProvider _xrProvider = _provider as LeapXRServiceProvider;

            deviceOffsetY = _xrProvider.deviceOffsetYAxis;
            deviceOffsetZ = _xrProvider.deviceOffsetZAxis;
            deviceTiltX = _xrProvider.deviceTiltXAxis;
        }

        Matrix4x4 imageMatWarp = Matrix4x4.identity;
        if (customValues)
        {
            imageMatWarp = Matrix4x4.TRS(translation, rotation, scale);
        }
        else
        {
            Matrix4x4 P = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
            Matrix4x4 V = Camera.main.worldToCameraMatrix;
            Matrix4x4 M = ImageRenderer.localToWorldMatrix;
            Matrix4x4 MVP = P * V * M;


            imageMatWarp *= _camera.projectionMatrix;
            imageMatWarp *= Matrix4x4.Rotate(Quaternion.Euler(deviceTiltX * 0.42f, 0, 0));
            imageMatWarp *= _camera.projectionMatrix.inverse;


            imageMatWarp *= MVP;
            imageMatWarp *= Matrix4x4.Scale(new Vector3(1, -1, 1));
            imageMatWarp *= Matrix4x4.Translate(new Vector3(0, deviceOffsetY * 0.55f, -deviceOffsetZ / (9 * deviceOffsetZ + 1f) * scale.z));
            imageMatWarp *= MVP.inverse;
        }

        Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", imageMatWarp);
    }
}