using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[LeapGuiFeatureName("Mesh")]
public class LeapGuiMeshFeature : LeapGuiFeature<LeapGuiMeshData> {
  public const string UV_0_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_0";
  public const string UV_1_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_1";
  public const string UV_2_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_2";
  public const string UV_3_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_UV_3";
  public const string COLORS_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_COLORS";
  public const string NORMALS_FEATURE = LeapGui.FEATURE_PREFIX + "VERTEX_NORMALS";

  public static string GetUvFeature(UVChannelFlags flags) {
    switch (flags) {
      case UVChannelFlags.UV0:
        return UV_0_FEATURE;
      case UVChannelFlags.UV1:
        return UV_1_FEATURE;
      case UVChannelFlags.UV2:
        return UV_2_FEATURE;
      case UVChannelFlags.UV3:
        return UV_3_FEATURE;
      default:
        throw new InvalidOperationException();
    }
  }

  public bool uv0;
  public bool uv1;
  public bool uv2;
  public bool uv3;
  public bool color;
  public Color tint;
  public bool normals;

  public IEnumerable<UVChannelFlags> enabledUvChannels {
    get {
      if (uv0) yield return UVChannelFlags.UV0;
      if (uv1) yield return UVChannelFlags.UV1;
      if (uv2) yield return UVChannelFlags.UV2;
      if (uv3) yield return UVChannelFlags.UV3;
    }
  }

#if UNITY_EDITOR
  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) {
    Rect line = rect.SingleLine();

    EditorGUI.LabelField(line, "Mesh");
    line = line.NextLine();

    line = line.Indent();

    Rect left, right;
    line.SplitHorizontally(out left, out right);

    uv0 = EditorGUI.ToggleLeft(left, "Uv0", uv0);
    uv1 = EditorGUI.ToggleLeft(right, "Uv1", uv1);
    line = line.NextLine();

    line.SplitHorizontally(out left, out right);
    uv2 = EditorGUI.ToggleLeft(left, "Uv2", uv2);
    uv3 = EditorGUI.ToggleLeft(right, "Uv3", uv3);
    line = line.NextLine();

    line.SplitHorizontally(out left, out right);
    color = EditorGUI.ToggleLeft(left, "Color", color);
    normals = EditorGUI.ToggleLeft(right, "Normals", normals);
    line = line.NextLine();

    if (color) {
      tint = EditorGUI.ColorField(line, "Tint", tint);
    }
  }

  public override float GetEditorHeight() {
    return EditorGUIUtility.singleLineHeight * (color ? 5 : 4);
  }
#endif
}
