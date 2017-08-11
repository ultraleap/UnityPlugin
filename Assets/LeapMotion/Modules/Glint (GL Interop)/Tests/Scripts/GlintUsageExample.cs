using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Leap.Unity.Glint.Tests {

  public class GlintUsageExample : MonoBehaviour {

    [Header("Takes a RenderTexture from")]
    public BlittingScript blittingScript;

    [Header("On success, puts data in _MainTex for")]
    public Material resultMaterial;

    [Header("Report profiling data to")]
    public TextMesh textMesh;

    private float[] _data;
    private Texture2D _previewTexture = null;

    private bool _firstUpdate = true;
    private StringBuilder _sb = new StringBuilder();

    void Update() {
      if (_firstUpdate) {
        initDataForTex(ref _data, blittingScript.renderTex);
        _previewTexture = new Texture2D(blittingScript.renderTex.width, blittingScript.renderTex.height);
        _previewTexture.filterMode = FilterMode.Point;
        resultMaterial.SetTexture("_MainTex", _previewTexture);

        Glint.requestWaitTimeInFrames = 1;
        Glint.RequestAsync(blittingScript.renderTex, _data, onDataRetrieved);

        _firstUpdate = false;
      }
    }

    // Callback that calls itself, for continuous (sequential, non-optimal) download.
    private void onDataRetrieved() {
      // Upon getting the callback, we look at the data and fill it into a texture
      // to display on the "CPU" side of the scene.
      fillTextureWithData(_data,
                          blittingScript.renderTex.width,
                          blittingScript.renderTex.height,
                          _previewTexture);

      _sb.Remove(0, _sb.Length);
      _sb.Append("Render thread map (ms):\t");
      _sb.Append(Glint.Profiling.lastRenderMapMs.ToString("F3"));
      _sb.Append("\n");
      _sb.Append("Render thread memcpy (ms):\t");
      _sb.Append(Glint.Profiling.lastRenderCopyMs.ToString("F3"));
      _sb.Append("\n");
      _sb.Append("Main thread memcpy (ms):\t");
      _sb.Append(Glint.Profiling.lastMainCopyMs.ToString("F3"));
      textMesh.text = _sb.ToString();

      Glint.RequestAsync(blittingScript.renderTex, _data, onDataRetrieved);
    }

    private static void initDataForTex(ref float[] data, RenderTexture tex) {
      data = new float[tex.width * tex.height * 4 /* four channels per pixel */];
    }

    private static Color[] _pixels = null;
    private static void fillTextureWithData(float[] data, int width, int height, Texture2D tex) {
      if (_pixels == null) {
        _pixels = new Color[width * height];
      }

      fillPixelsFromFloats(ref _pixels, data);

      tex.SetPixels(_pixels);
      tex.Apply();
    }

    private static void fillPixelsFromFloats(ref Color[] pixels, float[] data) {
      for (int i = 0; i + 3 < data.Length; i += 4) {
        pixels[i / 4] = new Color(data[i], data[i + 1], data[i + 2], data[i + 3]);
      }
    }

  }

}
