using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Texture")]
  [Serializable]
  public class LeapTextureFeature : LeapGraphicFeature<LeapTextureData> {

    //[EditTimeOnly]
    public string propertyName = "_MainTex";

    //[EditTimeOnly]
    public UVChannelFlags channel = UVChannelFlags.UV0;
  }
}
