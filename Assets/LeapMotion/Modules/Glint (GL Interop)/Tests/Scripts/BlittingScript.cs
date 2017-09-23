using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Glint.Tests {

  public class BlittingScript : MonoBehaviour {

    [Header("Initialization")]
    public int initTexSize = 32;

    [Header("GPU Updating")]
    public Material blitMaterial;

    [Header("Previewing")]
    public Material previewMaterial;

    private RenderTexture _renderTex;
    public RenderTexture renderTex {
      get { return _renderTex; }
    }

    void Start() {
      _renderTex = new RenderTexture(initTexSize, initTexSize, 0,
                                     RenderTextureFormat.ARGBFloat,
                                     RenderTextureReadWrite.Default);
      _renderTex.filterMode = FilterMode.Point;
      _renderTex.Create();

      previewMaterial.SetTexture("_MainTex", _renderTex);
    }

    void Update() {
      DoBlit(_renderTex);
    }

    private void DoBlit(RenderTexture renderTex) {
      Graphics.Blit(null, _renderTex, blitMaterial);
    }

  }

}
