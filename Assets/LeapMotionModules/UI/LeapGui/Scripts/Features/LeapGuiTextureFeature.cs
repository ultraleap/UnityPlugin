using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LeapGuiTextureFeature : LeapGuiFeature {

  [SerializeField]
  private string _propertyName = "_MainTex";

  [SerializeField]
  private UVChannelFlags _channel = UVChannelFlags.UV0;

  public override ScriptableObject CreateSettingsObject() {
    return ScriptableObject.CreateInstance<TextureSettings>();
  }

  public class TextureSettings : ElementSettings {

    [SerializeField]
    private Texture2D _texture;
  }
}
