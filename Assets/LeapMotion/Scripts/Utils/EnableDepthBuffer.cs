using UnityEngine;
using System.Collections;

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
