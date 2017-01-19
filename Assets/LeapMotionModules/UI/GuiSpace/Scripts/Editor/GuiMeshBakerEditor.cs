using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.AnimatedValues;

[CustomEditor(typeof(GuiMeshBaker))]
public class GuiMeshBakerEditor : Editor {

  AnimBool animColors;
  AnimBool animTex;
  AnimBool animMotion;

  void OnEnable() {
    animColors = new AnimBool(true);
    animColors.valueChanged.AddListener(Repaint);

    animTex = new AnimBool(true);
    animTex.valueChanged.AddListener(Repaint);

    animMotion = new AnimBool(true);
    animMotion.valueChanged.AddListener(Repaint);
  }
  public override void OnInspectorGUI() {
    base.OnInspectorGUI();
    /*
    animColors.target = EditorGUILayout.ToggleLeft("Enable Vertex Colors", animColors.target);
    if (EditorGUILayout.BeginFadeGroup(animColors.faded)) {
      EditorGUI.indentLevel++;
      EditorGUILayout.ColorField("Global Tint", Color.red);
      EditorGUI.indentLevel--;
    }
    EditorGUILayout.EndFadeGroup();

    animTex.target = EditorGUILayout.ToggleLeft("Enable Textures", animTex.target);
    if (EditorGUILayout.BeginFadeGroup(animTex.faded)) {
      EditorGUI.indentLevel++;
      EditorGUILayout.EnumPopup("Main Channel", UVChannelFlags.UV0);
      EditorGUILayout.EnumPopup("Secondary Channel", UVChannelFlags.UV1);
      EditorGUILayout.EnumPopup("Lightmap Channel", UVChannelFlags.UV2);
      EditorGUI.indentLevel--;
    }
    EditorGUILayout.EndFadeGroup();

    animMotion.target = EditorGUILayout.ToggleLeft("Enable Full Motion", animMotion.target);
    if (EditorGUILayout.BeginFadeGroup(animMotion.faded)) {
      EditorGUI.indentLevel++;
      EditorGUILayout.EnumPopup("Update Mode", UpdateMode.Lazy);
      EditorGUILayout.Toggle("Batch Leaf Element", true);
      EditorGUI.indentLevel--;
    }
    EditorGUILayout.EndFadeGroup();
     * */
  }

  public enum UpdateMode {
    EveryFrame,
    Lazy,
    ScriptControlled
  }

}
