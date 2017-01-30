using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[LeapGuiFeatureName("Texture")]
public class LeapGuiTextureFeature : LeapGuiFeature<LeapGuiTextureData> {
  public string propertyName = "_MainTex";
  public UVChannelFlags channel = UVChannelFlags.UV0;

#if UNITY_EDITOR
  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) {
    Rect line = rect.SingleLine();

    EditorGUI.LabelField(line, "Texture");
    line = line.NextLine();
    line = line.Indent();

    propertyName = EditorGUI.TextField(line, "Property Name", propertyName);
    line = line.NextLine();

    channel = (UVChannelFlags)EditorGUI.EnumPopup(line, "Uv Channel", channel);
  }

  public override float GetEditorHeight() {
    return EditorGUIUtility.singleLineHeight * 3;
  }
#endif
}
