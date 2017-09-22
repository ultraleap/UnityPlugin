using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Glint.Tests {

  [AddComponentMenu("")]
  public class GlintUsageExample : MonoBehaviour {

    [Header("Takes a RenderTexture from this script.")]
    public BlittingScript blittingScript;

    [Header("On success, puts data in _MainTex for this material.")]
    public Material resultMaterial;


    private byte[] _data;
    private Texture2D _previewTexture = null;

    private bool _firstUpdate = true;

    void Update() {
      if (_firstUpdate) {
        initDataForTex(ref _data, blittingScript.renderTex);
        _previewTexture = new Texture2D(blittingScript.renderTex.width, blittingScript.renderTex.height);
        _previewTexture.filterMode = FilterMode.Point;
        resultMaterial.SetTexture("_MainTex", _previewTexture);

        Glint.RequestTextureDownload(blittingScript.renderTex, _data, onDataRetrieved);

        _firstUpdate = false;
      }
    }

    // Callback that calls RequestAsync again, for "continuous" (sequential, non-optimal)
    // download.
    private void onDataRetrieved() {
      // Upon getting the callback, we look at the data and fill it into a texture
      // to display on the "CPU" side of the scene.
      fillTextureWithData(_data,
                          _previewTexture);

      //_sb.Remove(0, _sb.Length);
      //_sb.Append("Render thread map (ms):\t");
      //_sb.Append(ZZOLD_Glint.Profiling.lastRenderMapMs.ToString("F3"));
      //_sb.Append("\n");
      //_sb.Append("Render thread memcpy (ms):\t");
      //_sb.Append(ZZOLD_Glint.Profiling.lastRenderCopyMs.ToString("F3"));
      //_sb.Append("\n");
      //_sb.Append("Main thread memcpy (ms):\t");
      //_sb.Append(ZZOLD_Glint.Profiling.lastMainCopyMs.ToString("F3"));
      //textMesh.text = _sb.ToString();

      Glint.RequestTextureDownload(blittingScript.renderTex, _data, onDataRetrieved);
    }

    #region Support

    private static void initDataForTex(ref byte[] data, RenderTexture tex) {
      data = new byte[tex.width * tex.height * Glint.GetBytesPerPixel(tex)];
    }

    private static Color[] _pixels = null;
    private static void fillTextureWithData(byte[] data,
                                            Texture2D tex) {
      if (_pixels == null) {
        _pixels = new Color[tex.width * tex.height];
      }

      fillPixelsFromBytes_RGBAFloat32(data, ref _pixels);

      tex.SetPixels(_pixels);
      tex.Apply();
    }

    private static void fillPixelsFromBytes_RGBAFloat32(byte[] data, ref Color[] pixels) {
      unsafe {
        int increment = 16; // 4 bytes per color, 4 colors per pixel
        for (int i = 0; i + increment - 1 < data.Length; i += increment) {
          
        }
      }
      for (int i = 0; i + 3 < data.Length; i += 4) {
        pixels[i / 4] = new Color(data[i], data[i + 1], data[i + 2], data[i + 3]);
      }
    }

    #endregion

  }

}