/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public static class MeshRendererConversion {
    private const string CONTEXT_PATH = "CONTEXT/MeshRenderer/Convert To Leap Graphic Mesh";

    [MenuItem(CONTEXT_PATH)]
    public static void convert(MenuCommand command) {
      var graphicRenderer = (command.context as MeshRenderer).GetComponentInParent<LeapGraphicRenderer>();

      if (graphicRenderer.groups.Count == 0) {
        graphicRenderer.editor.CreateGroup(typeof(LeapBakedRenderer));
      }

      var group = graphicRenderer.groups[0];

      var graphics = new List<LeapMeshGraphic>();
      var meshRenderers = (command.context as MeshRenderer).GetComponentsInChildren<MeshRenderer>();
      foreach (var meshRenderer in meshRenderers) {
        var material = meshRenderer.sharedMaterial;
        if (material == null) continue;

        var shader = material.shader;
        if (shader == null) continue;

        var filter = meshRenderer.GetComponent<MeshFilter>();
        if (filter == null) continue;

        var mesh = filter.sharedMesh;
        if (mesh == null) continue;

        int propCount = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < propCount; i++) {
          if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv) {
            string propName = ShaderUtil.GetPropertyName(shader, i);

            if (material.GetTexture(propName) == null) continue;

            var feature = group.features.Query().
                                         OfType<LeapTextureFeature>().
                                         FirstOrDefault(f => f.propertyName == propName);

            if (feature == null) {
              feature = group.editor.AddFeature(typeof(LeapTextureFeature)) as LeapTextureFeature;
              feature.channel = UnityEngine.Rendering.UVChannelFlags.UV0;
              feature.propertyName = propName;
            }
          }
        }

        var graphic = meshRenderer.gameObject.AddComponent<LeapMeshGraphic>();
        Undo.RegisterCreatedObjectUndo(graphic, "Create Leap Mesh Graphic");

        group.TryAddGraphic(graphic);
        graphics.Add(graphic);
      }

      foreach (var graphic in graphics) {
        var meshRenderer = graphic.GetComponent<MeshRenderer>();
        var meshFilter = graphic.GetComponent<MeshFilter>();
        var material = meshRenderer.sharedMaterial;

        graphic.SetMesh(meshFilter.sharedMesh);

        foreach (var dataObj in graphic.featureData) {
          var textureData = dataObj as LeapTextureData;
          if (textureData == null) {
            continue;
          }

          var feature = textureData.feature as LeapTextureFeature;
          if (!material.HasProperty(feature.propertyName)) {
            continue;
          }

          Texture2D tex2d = material.GetTexture(feature.propertyName) as Texture2D;
          if (tex2d == null) {
            continue;
          }

          textureData.texture = tex2d;
        }

        Undo.DestroyObjectImmediate(meshRenderer);
        Undo.DestroyObjectImmediate(meshFilter);
      }

      group.renderer.editor.ScheduleRebuild();
    }

    [MenuItem(CONTEXT_PATH, validate = true)]
    public static bool convertValidate(MenuCommand command) {
      var graphicRenderer = (command.context as MeshRenderer).GetComponentInParent<LeapGraphicRenderer>();
      return graphicRenderer != null;
    }
  }
}
