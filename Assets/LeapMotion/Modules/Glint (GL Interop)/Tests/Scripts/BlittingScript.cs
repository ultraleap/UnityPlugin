using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ZZOLD_GLINT.Tests {

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
    private Texture2D _whiteTex;

    void Start() {
      _renderTex = new RenderTexture(initTexSize, initTexSize, 0,
                                     RenderTextureFormat.ARGBFloat,
                                     RenderTextureReadWrite.Default);
      _renderTex.filterMode = FilterMode.Point;
      _renderTex.Create();

      _whiteTex = new Texture2D(initTexSize, initTexSize, TextureFormat.RGBAFloat, false);
      _whiteTex.filterMode = FilterMode.Point;
      var pixels = new Color[initTexSize * initTexSize]; for (int i = 0; i < pixels.Length; i++) { pixels[i] = Color.white; }
      _whiteTex.SetPixels(pixels);
      _whiteTex.Apply();

      previewMaterial.SetTexture("_MainTex", _renderTex);
    }

    void Update() {
      DoBlit(_renderTex);
    }

    private void DoBlit(RenderTexture renderTex) {
      Graphics.Blit(_whiteTex, _renderTex, blitMaterial);
    }

  }

}
