using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Space;

[AddComponentMenu("")]
[LeapGuiTag("Dynamic")]
public class LeapGuiDynamicRenderer : LeapGuiMesherBase, ISupportsAddRemove {

  #region PRIVATE VARIABLES

  //Curved space
  private const string CURVED_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Curved_ElementParameters";
  private List<Matrix4x4> _curved_worldToAnchor = new List<Matrix4x4>();
  private List<Matrix4x4> _curved_meshTransforms = new List<Matrix4x4>();
  private List<Vector4> _curved_elementParameters = new List<Vector4>();
  #endregion

  public void OnAddElement() {
    //TODO
    if (Application.isPlaying) {
      throw new NotImplementedException();
    }
  }

  public void OnRemoveElement() {
    //TODO
    if (Application.isPlaying) {
      throw new NotImplementedException();
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

    if (gui.space == null) {
      using (new ProfilerSample("Draw Meshes")) {
        for (int i = 0; i < group.elements.Count; i++) {
          Graphics.DrawMesh(_meshes[i], group.elements[i].transform.localToWorldMatrix, _material, 0);
        }
      }
    } else if (gui.space is LeapRadialSpace) {
      var curvedSpace = gui.space as LeapRadialSpace;

      using (new ProfilerSample("Build Material Data")) {
        _curved_worldToAnchor.Clear();
        _curved_meshTransforms.Clear();
        _curved_elementParameters.Clear();
        for (int i = 0; i < _meshes.Count; i++) {
          var element = group.elements[i];
          var transformer = element.anchor.transformer;

          Vector3 guiLocalPos = gui.transform.InverseTransformPoint(element.transform.position);

          Matrix4x4 guiTransform = transform.localToWorldMatrix * transformer.GetTransformationMatrix(guiLocalPos);
          Matrix4x4 deform = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position - element.transform.position, Quaternion.identity, Vector3.one) * element.transform.localToWorldMatrix;
          Matrix4x4 total = guiTransform * deform;

          _curved_elementParameters.Add((transformer as IRadialTransformer).GetVectorRepresentation(element.transform));
          _curved_meshTransforms.Add(total);
          _curved_worldToAnchor.Add(guiTransform.inverse);
        }
      }

      using (new ProfilerSample("Upload Material Data")) {
        _material.SetFloat(SpaceProperties.RADIAL_SPACE_RADIUS, curvedSpace.radius);
        _material.SetMatrixArraySafe("_LeapGuiCurved_WorldToAnchor", _curved_worldToAnchor);
        _material.SetMatrix("_LeapGui_LocalToWorld", transform.localToWorldMatrix);
        _material.SetVectorArraySafe("_LeapGuiCurved_ElementParameters", _curved_elementParameters);
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
      _shader = Shader.Find("LeapGui/Defaults/Dynamic");
    }

    base.setupForBuilding();

    if (gui.space != null) {
      if (gui.space is LeapCylindricalSpace) {
        _material.EnableKeyword(SpaceProperties.CYLINDRICAL_FEATURE);
      } else if (gui.space is LeapSphericalSpace) {
        _material.EnableKeyword(SpaceProperties.SPHERICAL_FEATURE);
      }
    }
  }

  protected override void buildElement() {
    //Always start a new mesh for each element
    finishMesh();
    beginMesh();

    base.buildElement();
  }

  protected override bool doesRequireSpecialUv3() {
    return true;
  }

  protected override Vector3 elementVertToMeshVert(Vector3 vertex) {
    return vertex;
  }

  protected override void elementVertNormalToMeshVertNormal(Vector3 vertex,
                                                            Vector3 normal,
                                                        out Vector3 meshVert,
                                                        out Vector3 meshNormal) {
    meshVert = vertex;
    meshNormal = normal;
  }

  protected override Vector3 blendShapeDelta(Vector3 shapeVert, Vector3 originalVert) {
    //TODO, optimize this, i'm sure it could be optimized.
    Vector3 worldVert = _currElement.transform.TransformPoint(shapeVert);
    Vector3 guiVert = gui.transform.InverseTransformPoint(worldVert);
    shapeVert = guiVert - gui.transform.InverseTransformPoint(_currElement.transform.position);

    Vector3 worldVert2 = _currElement.transform.TransformPoint(originalVert);
    Vector3 guiVert2 = gui.transform.InverseTransformPoint(worldVert2);
    originalVert = guiVert2 - gui.transform.InverseTransformPoint(_currElement.transform.position);

    return shapeVert - originalVert;
  }
}
