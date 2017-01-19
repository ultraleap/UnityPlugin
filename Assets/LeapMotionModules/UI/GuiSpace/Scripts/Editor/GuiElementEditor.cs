using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapElement))]
public class LeapElementEditor : CustomEditorBase {

  private GuiMeshBaker baker;
  private LeapElement element;
  private List<LeapElement> elements;
  private List<GuiMeshBaker> bakers;

  private SerializedProperty textureGUIDs;
  private int minTextureChannels;

  private SerializedProperty vertexColor;
  private bool vertexColorsEnabledForAll;

  private SerializedProperty tintColor;
  private bool tintingEnabledForAll;

  private SerializedProperty blendShape;
  private bool blendShapesEnabledForAll;

  protected override void OnEnable() {
    base.OnEnable();

    element = target as LeapElement;
    elements = targets.Query().OfType<LeapElement>().ToList();
    bakers = elements.Query().Select(e => e.baker).ToList();

    textureGUIDs = serializedObject.FindProperty("_textureGUIDs");

    minTextureChannels = int.MaxValue;
    foreach (var baker in bakers) {
      minTextureChannels = Mathf.Min(minTextureChannels, baker.textureChannels);
    }

    while (textureGUIDs.arraySize < minTextureChannels) {
      textureGUIDs.InsertArrayElementAtIndex(textureGUIDs.arraySize);
    }

    vertexColor = serializedObject.FindProperty("_vertexColor");
    vertexColorsEnabledForAll = bakers.Query().All(baker => baker.enableVertexColors);

    tintColor = serializedObject.FindProperty("_tint");
    tintingEnabledForAll = bakers.Query().All(baker => baker.enableTinting);

    blendShape = serializedObject.FindProperty("_blendShape");
    blendShapesEnabledForAll = bakers.Query().All(baker => baker.enableBlendShapes);

    serializedObject.ApplyModifiedProperties();
  }

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    for (int i = 0; i < minTextureChannels; i++) {
      SerializedProperty texGUID = textureGUIDs.GetArrayElementAtIndex(i);
      string label = "Texture " + i;

      EditorGUI.showMixedValue = texGUID.hasMultipleDifferentValues;
      Texture2D currTex = element.GetTexture(i);
      Object newTexture = EditorGUILayout.ObjectField(label,
                                                      currTex,
                                                      typeof(Texture2D),
                                                      allowSceneObjects: false,
                                                      options: GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
      EditorGUI.showMixedValue = false;

      if (currTex != newTexture) {
        if (newTexture == null) {
          texGUID.stringValue = "";
        } else {
          string newPath = AssetDatabase.GetAssetPath(newTexture);
          texGUID.stringValue = AssetDatabase.AssetPathToGUID(newPath);
        }
      }
    }

    if (vertexColorsEnabledForAll) {
      EditorGUILayout.PropertyField(vertexColor);
    }

    if (tintingEnabledForAll) {
      EditorGUILayout.PropertyField(tintColor);
    }

    if (blendShapesEnabledForAll) {
      EditorGUILayout.PropertyField(blendShape);
    }

    serializedObject.ApplyModifiedProperties();
  }


}
