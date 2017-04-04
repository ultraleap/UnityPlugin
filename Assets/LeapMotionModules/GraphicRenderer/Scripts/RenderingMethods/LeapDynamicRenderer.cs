using System;
using System.Collections.Generic;
using UnityEngine;
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

    public void OnAddGraphic(LeapGraphic graphic) {
      beginMesh();
      _currGraphic = graphic as LeapMeshGraphicBase;
      _currIndex = _meshes.Count;
      finishMesh();
    }

    public void OnRemoveGraphic(LeapGraphic graphic, int graphicIndex) {
      _meshes.RemoveMesh(graphicIndex);
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
        _shader = Shader.Find("Leap Motion/Graphic Renderer/Defaults/Dynamic");
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
      finishMesh();
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
      Vector3 worldVert = _currGraphic.transform.TransformPoint(shapeVert);
      Vector3 localVert = renderer.transform.InverseTransformPoint(worldVert);
      shapeVert = localVert - renderer.transform.InverseTransformPoint(_currGraphic.transform.position);

      Vector3 worldVert2 = _currGraphic.transform.TransformPoint(originalVert);
      Vector3 localVert2 = renderer.transform.InverseTransformPoint(worldVert2);
      originalVert = localVert2 - renderer.transform.InverseTransformPoint(_currGraphic.transform.position);

      return shapeVert - originalVert;
    }
  }
}
