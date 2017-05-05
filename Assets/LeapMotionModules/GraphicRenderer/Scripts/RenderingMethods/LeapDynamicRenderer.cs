using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  [AddComponentMenu("")]
  [LeapGraphicTag("Dynamic")]
  public class LeapDynamicRenderer : LeapMesherBase, ISupportsAddRemove {

    #region PRIVATE VARIABLES

    //Curved space
    private const string CURVED_PARAMETERS = LeapGraphicRenderer.PROPERTY_PREFIX + "Curved_GraphicParameters";
    private List<Matrix4x4> _curved_worldToAnchor = new List<Matrix4x4>();
    private List<Matrix4x4> _curved_meshTransforms = new List<Matrix4x4>();
    private List<Vector4> _curved_graphicParameters = new List<Vector4>();
    #endregion

    public void OnAddRemoveGraphics(List<int> dirtyIndexes) {
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
      base.OnUpdateRenderer();

      for (int i = 0; i < group.graphics.Count; i++) {
        var graphic = group.graphics[i];
        if (graphic.isRepresentationDirty) {
          beginMesh(_meshes[i]);
          _generation.graphic = graphic as LeapMeshGraphic;
          _generation.graphicIndex = i;
          _generation.graphicId = i;
          base.buildGraphic();
          finishMesh();
          _generation.mesh = null;
        }
      }

      if (renderer.space == null) {
        using (new ProfilerSample("Draw Meshes")) {
          for (int i = 0; i < group.graphics.Count; i++) {
            Graphics.DrawMesh(_meshes[i], group.graphics[i].transform.localToWorldMatrix, _material, 0);
          }
        }
      } else if (renderer.space is LeapRadialSpace) {
        var curvedSpace = renderer.space as LeapRadialSpace;

        using (new ProfilerSample("Build Material Data")) {
          _curved_worldToAnchor.Clear();
          _curved_meshTransforms.Clear();
          _curved_graphicParameters.Clear();
          for (int i = 0; i < _meshes.Count; i++) {
            var graphic = group.graphics[i];
            var transformer = graphic.anchor.transformer;

            Vector3 localPos = renderer.transform.InverseTransformPoint(graphic.transform.position);

            Matrix4x4 mainTransform = transform.localToWorldMatrix * transformer.GetTransformationMatrix(localPos);
            Matrix4x4 deform = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position - graphic.transform.position, Quaternion.identity, Vector3.one) * graphic.transform.localToWorldMatrix;
            Matrix4x4 total = mainTransform * deform;

            _curved_graphicParameters.Add((transformer as IRadialTransformer).GetVectorRepresentation(graphic.transform));
            _curved_meshTransforms.Add(total);
            _curved_worldToAnchor.Add(mainTransform.inverse);
          }
        }

        using (new ProfilerSample("Upload Material Data")) {
          _material.SetFloat(SpaceProperties.RADIAL_SPACE_RADIUS, curvedSpace.radius);
          _material.SetMatrixArraySafe("_GraphicRendererCurved_WorldToAnchor", _curved_worldToAnchor);
          _material.SetMatrix("_GraphicRenderer_LocalToWorld", transform.localToWorldMatrix);
          _material.SetVectorArraySafe("_GraphicRendererCurved_GraphicParameters", _curved_graphicParameters);
        }

        using (new ProfilerSample("Draw Meshes")) {
          for (int i = 0; i < _meshes.Count; i++) {
            Graphics.DrawMesh(_meshes[i], _curved_meshTransforms[i], _material, 0);
          }
        }
      }
    }

    protected override void setupForBuilding() {
      if (_shader == null) {
        _shader = Shader.Find("Leap Motion/Graphic Renderer/Unlit/Dynamic");
      }

      base.setupForBuilding();

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
      finishAndAddMesh();
      beginMesh();

      base.buildGraphic();
    }

    protected override bool doesRequireSpecialUv3() {
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
      //TODO, optimize this, i'm sure it could be optimized.
      Vector3 worldVert = _generation.graphic.transform.TransformPoint(shapeVert);
      Vector3 localVert = renderer.transform.InverseTransformPoint(worldVert);
      shapeVert = localVert - renderer.transform.InverseTransformPoint(_generation.graphic.transform.position);

      Vector3 worldVert2 = _generation.graphic.transform.TransformPoint(originalVert);
      Vector3 localVert2 = renderer.transform.InverseTransformPoint(worldVert2);
      originalVert = localVert2 - renderer.transform.InverseTransformPoint(_generation.graphic.transform.position);

      return shapeVert - originalVert;
    }
  }
}
