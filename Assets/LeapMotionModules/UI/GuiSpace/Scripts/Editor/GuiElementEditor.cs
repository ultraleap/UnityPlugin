using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Leap.Unity;
using Leap.Unity.Query;

namespace Leap.Unity.Gui.Space {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(LeapElement))]
  public class LeapElementEditor : CustomEditorBase {

    private GuiMeshBaker baker;
    private LeapElement element;
    private List<LeapElement> elements;
    private List<GuiMeshBaker> bakers;

    private SerializedProperty meshGuid;

    private SerializedProperty textures;
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

      var mesh = serializedObject.FindProperty("_mesh");
      meshGuid = mesh.FindPropertyRelative("_guid");

      textures = serializedObject.FindProperty("_textures");

      minTextureChannels = int.MaxValue;
      foreach (var baker in bakers) {
        minTextureChannels = Mathf.Min(minTextureChannels, baker.textureChannels);
      }

      while (textures.arraySize < minTextureChannels) {
        textures.InsertArrayElementAtIndex(textures.arraySize);
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

      Mesh currMesh = element.GetMesh();
      Object newMesh = EditorGUILayout.ObjectField("Mesh",
                                                   currMesh,
                                                   typeof(Mesh),
                                                   allowSceneObjects: false,
                                                   options: GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));

      if (newMesh != currMesh) {
        if (newMesh == null) {
          meshGuid.stringValue = "";
        } else {
          string newPath = AssetDatabase.GetAssetPath(newMesh);
          meshGuid.stringValue = AssetDatabase.AssetPathToGUID(newPath);
        }
      }

      for (int i = 0; i < minTextureChannels; i++) {
        SerializedProperty noReferenceElement = textures.GetArrayElementAtIndex(i);
        SerializedProperty guid = noReferenceElement.FindPropertyRelative("_guid");
        string label = "Texture " + i;

        EditorGUI.showMixedValue = guid.hasMultipleDifferentValues;
        Texture2D currTex = element.GetTexture(i);
        Object newTexture = EditorGUILayout.ObjectField(label,
                                                        currTex,
                                                        typeof(Texture2D),
                                                        allowSceneObjects: false,
                                                        options: GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
        EditorGUI.showMixedValue = false;

        if (currTex != newTexture) {
          if (newTexture == null) {
            guid.stringValue = "";
          } else {
            string newPath = AssetDatabase.GetAssetPath(newTexture);
            guid.stringValue = AssetDatabase.AssetPathToGUID(newPath);
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
}
