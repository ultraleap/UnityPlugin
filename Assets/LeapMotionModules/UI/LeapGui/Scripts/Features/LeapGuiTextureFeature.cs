using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class LeapGuiTextureFeature : LeapGuiFeature<LeapGuiTextureData> {
  public string propertyName = "_MainTex";
  public UVChannelFlags channel = UVChannelFlags.UV0;
}
