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
    private bool _readyForAnotherRequest = true;

    void Update() {
      if (_firstUpdate) {
        initDataForTex(ref _data, blittingScript.renderTex);
        _previewTexture = new Texture2D(blittingScript.renderTex.width, blittingScript.renderTex.height, TextureFormat.RGBAFloat, false);
        _previewTexture.filterMode = FilterMode.Point;
        resultMaterial.SetTexture("_MainTex", _previewTexture);

        _firstUpdate = false;
      }

      if (_readyForAnotherRequest) {
        Glint.RequestTextureDownload(blittingScript.renderTex, _data, onDataRetrieved);

        _readyForAnotherRequest = false;
      }
    }

    // Callback that calls RequestAsync again, for "continuous" (sequential, non-optimal)
    // download.
    private void onDataRetrieved() {
      // Upon getting the callback, we look at the data and fill it into a texture
      // to display on the "CPU" side of the scene.
      fillTextureWithData(_data,
                          _previewTexture);

      // TODO: Much faster if this is made to work
      // _previewTexture.LoadRawTextureData(_data);

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

      _readyForAnotherRequest = true;
    }

    #region Support

    private static void initDataForTex(ref byte[] data, RenderTexture tex) {
      data = new byte[tex.width * tex.height * Glint.GetBytesPerPixel(tex)];
    }

    private static void fillTextureWithData(byte[] data,
                                            Texture2D tex) {
      tex.LoadRawTextureData(data);
      tex.Apply();
    }

    private static void fillFloatsFromBytes(byte[] bytes, float[] floats) {
      System.Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
    }

    private static void fillPixelsFromFloats(float[] floats, Color[] pixels) {
      for (int i = 0; i + 3 < floats.Length; i += 4) {
        pixels[i / 4] = new Color(floats[i], floats[i + 1], floats[i + 2], floats[i + 3]);
      }
    }

    #endregion

  }

}