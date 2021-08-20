/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;

namespace Leap.Unity{
  [ExecuteInEditMode]
  public class EnableDepthBuffer : MonoBehaviour {
    public const string DEPTH_TEXTURE_VARIANT_NAME = "USE_DEPTH_TEXTURE";
  
    [SerializeField]
    private DepthTextureMode _depthTextureMode = DepthTextureMode.Depth;
  
    void Awake() {
      GetComponent<Camera>().depthTextureMode = _depthTextureMode;
  
      if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) &&
          _depthTextureMode != DepthTextureMode.None) {
        Shader.EnableKeyword(DEPTH_TEXTURE_VARIANT_NAME);
      } else {
        Shader.DisableKeyword(DEPTH_TEXTURE_VARIANT_NAME);
      }
    }
  }
}
