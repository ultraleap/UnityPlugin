/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Dynamic")]
  [Serializable]
  public class LeapDynamicRenderer : LeapMesherBase, ISupportsAddRemove {
    public const string DEFAULT_SHADER = "LeapMotion/GraphicRenderer/Unlit/Dynamic Transparent";

    #region PRIVATE VARIABLES

    //Curved space
    private const string CURVED_PARAMETERS = LeapGraphicRenderer.PROPERTY_PREFIX + "Curved_GraphicParameters";
    private List<Matrix4x4> _curved_worldToAnchor = new List<Matrix4x4>();
    private List<Vector4> _curved_graphicParameters = new List<Vector4>();
    #endregion

    public void OnAddRemoveGraphics(List<int> dirtyIndexes) {
      MeshCache.Clear();

      while (_meshes.Count > group.graphics.Count) {
        _meshes.RemoveMesh(_meshes.Count - 1);
      }

      while (_meshes.Count < group.graphics.Count) {
        beginMesh();
        _generation.graphic = group.graphics[_meshes.Count] as LeapMeshGraphicBase;
        _generation.graphicIndex = _meshes.Count;
        _generation.graphicId = _meshes.Count;
        base.buildGraphic();
        finishAndAddMesh();
      }

      foreach (var dirtyIndex in dirtyIndexes) {
        beginMesh(_meshes[dirtyIndex]);
        _generation.graphic = group.graphics[dirtyIndex] as LeapMeshGraphicBase;
        _generation.graphicIndex = dirtyIndex;
        _generation.graphicId = dirtyIndex;
        base.buildGraphic();
        finishMesh();
        _generation.mesh = null;
      }

      prepareMaterial();
    }

    public override SupportInfo GetSpaceSupportInfo(LeapSpace space) {
      if (space == null ||
          space is LeapCylindricalSpace ||
          space is LeapSphericalSpace) {
        return SupportInfo.FullSupport();
      } else {
        return SupportInfo.Error("Dynamic Renderer does not support " + space.GetType().Name);
      }
    }

    public override void OnUpdateRenderer() {
      using (new ProfilerSample("Update Dynamic Renderer")) {
        base.OnUpdateRenderer();

        using (new ProfilerSample("Check For Dirty Graphics")) {
          for (int i = 0; i < group.graphics.Count; i++) {
            var graphic = group.graphics[i];
            if (graphic.isRepresentationDirty || _meshes[i] == null) {
              beginMesh(_meshes[i]);
              _generation.graphic = graphic as LeapMeshGraphicBase;
              _generation.graphicIndex = i;
              _generation.graphicId = i;
              base.buildGraphic();
              finishMesh();
              _meshes[i] = _generation.mesh;
              _generation.mesh = null;

              graphic.isRepresentationDirty = false;
            }
          }
        }

        if (renderer.space == null) {
          using (new ProfilerSample("Draw Meshes")) {
            Assert.AreEqual(group.graphics.Count, _meshes.Count);
            for (int i = 0; i < group.graphics.Count; i++) {
              if (!group.graphics[i].isActiveAndEnabled) {
                continue;
              }

              drawMesh(_meshes[i], group.graphics[i].transform.localToWorldMatrix);
            }
          }
        } else if (renderer.space is LeapRadialSpace) {
          var curvedSpace = renderer.space as LeapRadialSpace;
          using (new ProfilerSample("Build Material Data And Draw Meshes")) {
            _curved_worldToAnchor.Clear();
            _curved_graphicParameters.Clear();
            for (int i = 0; i < _meshes.Count; i++) {
              var graphic = group.graphics[i];
              if (!graphic.isActiveAndEnabled) {
                _curved_graphicParameters.Add(Vector4.zero);
                _curved_worldToAnchor.Add(Matrix4x4.identity);
                continue;
              }

              var transformer = graphic.anchor.transformer;

              Vector3 localPos = renderer.transform.InverseTransformPoint(graphic.transform.position);

              Matrix4x4 mainTransform = renderer.transform.localToWorldMatrix * transformer.GetTransformationMatrix(localPos);
              Matrix4x4 deform = renderer.transform.worldToLocalMatrix * Matrix4x4.TRS(renderer.transform.position - graphic.transform.position, Quaternion.identity, Vector3.one) * graphic.transform.localToWorldMatrix;
              Matrix4x4 total = mainTransform * deform;

              _curved_graphicParameters.Add((transformer as IRadialTransformer).GetVectorRepresentation(graphic.transform));
              _curved_worldToAnchor.Add(mainTransform.inverse);

              //Safe to do this before we upload material data
              //meshes are drawn at end of frame anyway!
              drawMesh(_meshes[i], total);
            }
          }

          using (new ProfilerSample("Upload Material Data")) {
            _material.SetFloat(SpaceProperties.RADIAL_SPACE_RADIUS, curvedSpace.radius);
            _material.SetMatrixArraySafe("_GraphicRendererCurved_WorldToAnchor", _curved_worldToAnchor);
            _material.SetMatrix("_GraphicRenderer_LocalToWorld", renderer.transform.localToWorldMatrix);
            _material.SetVectorArraySafe("_GraphicRendererCurved_GraphicParameters", _curved_graphicParameters);
          }
        }
      }
    }

#if UNITY_EDITOR
    public override void OnEnableRendererEditor() {
      base.OnEnableRendererEditor();

      _shader = Shader.Find(DEFAULT_SHADER);
    }
#endif

    protected override void prepareMaterial() {
      if (_shader == null) {
        _shader = Shader.Find(DEFAULT_SHADER);
      }

      base.prepareMaterial();

      if (renderer.space != null) {
        if (renderer.space is LeapCylindricalSpace) {
          _material.EnableKeyword(SpaceProperties.CYLINDRICAL_FEATURE);
        } else if (renderer.space is LeapSphericalSpace) {
          _material.EnableKeyword(SpaceProperties.SPHERICAL_FEATURE);
        }
      }
    }

    protected override void buildGraphic() {
      //Always start a new mesh for each graphic
      if (!_generation.isGenerating) {
        beginMesh();
      }

      base.buildGraphic();

      finishAndAddMesh(deleteEmptyMeshes: false);
    }

    protected override bool doesRequireVertInfo() {
      return true;
    }

    protected override Vector3 graphicVertToMeshVert(Vector3 vertex) {
      return vertex;
    }

    protected override void graphicVertNormalToMeshVertNormal(Vector3 vertex,
                                                              Vector3 normal,
                                                          out Vector3 meshVert,
                                                          out Vector3 meshNormal) {
      meshVert = vertex;
      meshNormal = normal;
    }

    protected override Vector3 blendShapeDelta(Vector3 shapeVert, Vector3 originalVert) {
      return shapeVert - originalVert;
    }
  }
}
