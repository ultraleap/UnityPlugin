/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
