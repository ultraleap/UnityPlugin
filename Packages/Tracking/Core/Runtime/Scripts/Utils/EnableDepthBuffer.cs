/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{
    public class EnableDepthBuffer : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        public const string DEPTH_TEXTURE_VARIANT_NAME = "USE_DEPTH_TEXTURE";

        [SerializeField]
        private DepthTextureMode _depthTextureMode = DepthTextureMode.Depth;

        void Awake()
        {
            if (_camera == null)
            {
                Debug.LogError(_camera.gameObject);
                return;
            }

            _camera.depthTextureMode = _depthTextureMode;

            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) &&
                _depthTextureMode != DepthTextureMode.None)
            {
                Shader.EnableKeyword(DEPTH_TEXTURE_VARIANT_NAME);
            }
            else
            {
                Shader.DisableKeyword(DEPTH_TEXTURE_VARIANT_NAME);
            }
        }
    }
}