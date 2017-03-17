using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;

[AddComponentMenu("")]
[LeapGuiTag("Texture")]
public class LeapGuiTextureFeature : LeapGuiFeature<LeapGuiTextureData> {

  [EditTimeOnly]
  public string propertyName = "_MainTex";

  [EditTimeOnly]
  public UVChannelFlags channel = UVChannelFlags.UV0;

#if UNITY_EDITOR
  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) {
    Rect line = rect.SingleLine();

    propertyName = EditorGUI.TextField(line, "Property Name", propertyName);
    line = line.NextLine();

    channel = (UVChannelFlags)EditorGUI.EnumPopup(line, "Uv Channel", channel);
  }

  public override float GetEditorHeight() {
    return EditorGUIUtility.singleLineHeight * 2;
  }
#endif
}
