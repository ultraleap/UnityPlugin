//using System.Collections;
//using System.Collections.Generic;
//using System.Text;
//using UnityEngine;

//namespace Leap.Unity.ZZOLD_GLINT.Tests {

//  public class GlintUsageExample_ZZOLD : MonoBehaviour {

//    [Header("Takes a RenderTexture from this script.")]
//    public BlittingScript blittingScript;

//    [Header("On success, puts data in _MainTex for this material.")]
//    public Material resultMaterial;

//    [Header("Report profiling data to this textMesh.")]
//    public TextMesh textMesh;

//    private float[] _data;
//    private Texture2D _previewTexture = null;

//    private bool _firstUpdate = true;
//    private StringBuilder _sb = new StringBuilder();

//    void Update() {
//      if (_firstUpdate) {
//        initDataForTex(ref _data, blittingScript.renderTex);
//        _previewTexture = new Texture2D(blittingScript.renderTex.width, blittingScript.renderTex.height);
//        _previewTexture.filterMode = FilterMode.Point;
//        resultMaterial.SetTexture("_MainTex", _previewTexture);

//        ZZOLD_Glint.requestWaitTimeInFrames = 1;
//        ZZOLD_Glint.RequestAsync(blittingScript.renderTex, _data, onDataRetrieved);

//        _firstUpdate = false;
//      }
//    }

//    // Callback that calls RequestAsync again, for "continuous" (sequential, non-optimal)
//    // download.
//    private void onDataRetrieved() {
//      // Upon getting the callback, we look at the data and fill it into a texture
//      // to display on the "CPU" side of the scene.
//      fillTextureWithData(_data,
//                          blittingScript.renderTex.width,
//                          blittingScript.renderTex.height,
//                          _previewTexture);

//      _sb.Remove(0, _sb.Length);
//      _sb.Append("Render thread map (ms):\t");
//      _sb.Append(ZZOLD_Glint.Profiling.lastRenderMapMs.ToString("F3"));
//      _sb.Append("\n");
//      _sb.Append("Render thread memcpy (ms):\t");
//      _sb.Append(ZZOLD_Glint.Profiling.lastRenderCopyMs.ToString("F3"));
//      _sb.Append("\n");
//      _sb.Append("Main thread memcpy (ms):\t");
//      _sb.Append(ZZOLD_Glint.Profiling.lastMainCopyMs.ToString("F3"));
//      textMesh.text = _sb.ToString();

//      ZZOLD_Glint.RequestAsync(blittingScript.renderTex, _data, onDataRetrieved);
//    }

//    private static void initDataForTex(ref byte[] data, RenderTexture tex) {
//      data = new byte[tex.width * tex.height * sizeof(float) * 4 /* four channels per pixel */];
//    }

//    private static Color[] _pixels = null;
//    private static void fillTextureWithData(byte[] data, int width, int height, Texture2D tex) {
//      if (_pixels == null) {
//        _pixels = new Color[width * height];
//      }

//      fillPixelsFromBytes_RGBAFloat32(data, ref _pixels);

//      tex.SetPixels(_pixels);
//      tex.Apply();
//    }

//    private static void fillPixelsFromBytes_RGBAFloat32(byte[] data, ref Color[] pixels) {
//      for (int i = 0; i + 3 < data.Length; i += 4) {
//        pixels[i / 4] = new Color(data[i], data[i + 1], data[i + 2], data[i + 3]);
//      }
//    }

//  }

//}
