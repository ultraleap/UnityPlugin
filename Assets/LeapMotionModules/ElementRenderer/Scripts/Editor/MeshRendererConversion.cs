using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

public static class MeshRendererConversion {
  private const string CONTEXT_PATH = "CONTEXT/MeshRenderer/Convert To Gui Element";

  [MenuItem(CONTEXT_PATH)]
  public static void convert(MenuCommand command) {
    var gui = (command.context as MeshRenderer).GetComponentInParent<LeapGui>();

    if (gui.groups.Count == 0) {
      gui.CreateGroup(typeof(LeapGuiBakedRenderer));
    }

    var group = gui.groups[0];

    var elements = new List<LeapGuiMeshElement>();
    var renderers = (command.context as MeshRenderer).GetComponentsInChildren<MeshRenderer>();
    foreach (var renderer in renderers) {
      var material = renderer.sharedMaterial;
      if (material == null) continue;

      var shader = material.shader;
      if (shader == null) continue;

      var filter = renderer.GetComponent<MeshFilter>();
      if (filter == null) continue;

      var mesh = filter.sharedMesh;
      if (mesh == null) continue;

      int propCount = ShaderUtil.GetPropertyCount(shader);
      for (int i = 0; i < propCount; i++) {
        if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
          string propName = ShaderUtil.GetPropertyName(shader, i);

          if (material.GetTexture(propName) == null) continue;

          var feature = group.features.Query().
                                       OfType<LeapGuiTextureFeature>().
                                       FirstOrDefault(f => f.propertyName == propName);

          if (feature == null) {
            feature = group.AddFeature(typeof(LeapGuiTextureFeature)) as LeapGuiTextureFeature;
            feature.channel = UnityEngine.Rendering.UVChannelFlags.UV0;
            feature.propertyName = propName;
          }
        }
      }

      var element = renderer.gameObject.AddComponent<LeapGuiMeshElement>();
      Undo.RegisterCreatedObjectUndo(element, "Create Mesh Element");

      group.TryAddElement(element);
      elements.Add(element);
    }

    foreach (var element in elements) {
      var renderer = element.GetComponent<MeshRenderer>();
      var filter = element.GetComponent<MeshFilter>();
      var material = renderer.sharedMaterial;

      element.SetMesh(filter.sharedMesh);

      foreach (var dataObj in element.data) {
        var textureData = dataObj as LeapGuiTextureData;
        if (textureData == null) {
          continue;
        }

        var feature = textureData.feature as LeapGuiTextureFeature;
        if (!material.HasProperty(feature.propertyName)) {
          continue;
        }

        Texture2D tex2d = material.GetTexture(feature.propertyName) as Texture2D;
        if (tex2d == null) {
          continue;
        }
        
        textureData.texture = tex2d;
      }

      Undo.DestroyObjectImmediate(renderer);
      Undo.DestroyObjectImmediate(filter);
    }

    group.gui.ScheduleEditorUpdate();
  }

  [MenuItem(CONTEXT_PATH, validate = true)]
  public static bool convertValidate(MenuCommand command) {
    var gui = (command.context as MeshRenderer).GetComponentInParent<LeapGui>();
    return gui != null;
  }
}
