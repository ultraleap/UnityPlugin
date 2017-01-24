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

    private SerializedProperty sprites;
    private int minTextureChannels;

    private SerializedProperty vertexColor;
    private bool vertexColorsEnabledForAll;

    private SerializedProperty tintColor;
    private bool tintingEnabledForAll;

    private SerializedProperty blendShape;
    private SerializedProperty blendShapeAmount;
    private bool blendShapesEnabledForAll;

    protected override void OnEnable() {
      base.OnEnable();

      element = target as LeapElement;
      elements = targets.Query().OfType<LeapElement>().ToList();
      bakers = elements.Query().Select(e => e.baker).ToList();

      var mesh = serializedObject.FindProperty("_mesh");
      meshGuid = mesh.FindPropertyRelative("_guid");

      sprites = serializedObject.FindProperty("_sprites");

      minTextureChannels = int.MaxValue;
      foreach (var baker in bakers) {
        minTextureChannels = Mathf.Min(minTextureChannels, baker.textureChannels);
      }

      while (sprites.arraySize < minTextureChannels) {
        sprites.InsertArrayElementAtIndex(sprites.arraySize);
      }

      vertexColor = serializedObject.FindProperty("_vertexColor");
      vertexColorsEnabledForAll = bakers.Query().All(baker => baker.enableVertexColors);

      tintColor = serializedObject.FindProperty("_tint");
      tintingEnabledForAll = bakers.Query().All(baker => baker.enableTinting);

      blendShape = serializedObject.FindProperty("_blendShape");
      blendShapeAmount = serializedObject.FindProperty("_blendShapeAmount");
      blendShapesEnabledForAll = bakers.Query().All(baker => baker.enableBlendShapes);

      serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      using (new EditorGUI.DisabledGroupScope(elements.Query().Any(e => e.IsUsingProceduralMeshSource()))) {
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
      }

      for (int i = 0; i < minTextureChannels; i++) {
        SerializedProperty sprite = sprites.GetArrayElementAtIndex(i);
        EditorGUILayout.PropertyField(sprite, new GUIContent("Sprite " + i));
      }

      if (vertexColorsEnabledForAll) {
        EditorGUILayout.PropertyField(vertexColor);
      }

      if (tintingEnabledForAll) {
        EditorGUILayout.PropertyField(tintColor);
      }

      if (blendShapesEnabledForAll) {
        EditorGUILayout.PropertyField(blendShape);
        EditorGUILayout.PropertyField(blendShapeAmount);
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
