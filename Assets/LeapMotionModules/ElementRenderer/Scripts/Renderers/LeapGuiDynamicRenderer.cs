using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

[AddComponentMenu("")]
[LeapGuiTag("Dynamic")]
public class LeapGuiDynamicRenderer : LeapGuiMesherBase,
  ISupportsAddRemove {

  #region PRIVATE VARIABLES

  //Cylindrical space
  private const string CYLINDRICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Cylindrical_ElementParameters";
  private List<Matrix4x4> _cylindrical_worldToAnchor = new List<Matrix4x4>();
  private List<Matrix4x4> _cylindrical_meshTransforms = new List<Matrix4x4>();
  private List<Vector4> _cylindrical_elementParameters = new List<Vector4>();

  //Spherical space
  private const string SPHERICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Spherical_ElementParameters";
  private List<Matrix4x4> _spherical_worldToAnchor = new List<Matrix4x4>();
  private List<Matrix4x4> _spherical_meshTransforms = new List<Matrix4x4>();
  private List<Vector4> _spherical_elementParameters = new List<Vector4>();
  #endregion

  public void OnAddElements(List<LeapGuiElement> elements, List<int> indexes) {
    //TODO, this is super slow and sad
    OnUpdateRendererEditor();
  }

  public void OnRemoveElements(List<int> toRemove) {
    //TODO, this is super slow and sad
    OnUpdateRendererEditor();
  }

  public override SupportInfo GetSpaceSupportInfo(LeapGuiSpace space) {
    if (space is LeapGuiRectSpace ||
        space is LeapGuiCylindricalSpace ||
        space is LeapGuiSphericalSpace) {
      return SupportInfo.FullSupport();
    } else {
      return SupportInfo.Error("Dynamic Renderer does not support " + space.GetType().Name);
    }
  }

  public override void OnUpdateRenderer() {
    //TODO: combine this logic somehow, lots of duplication
    if (gui.space is LeapGuiRectSpace) {
      for (int i = 0; i < gui.elements.Count; i++) {
        Graphics.DrawMesh(_meshes[i], gui.elements[i].transform.localToWorldMatrix, _material, 0);
      }
    } else if (gui.space is LeapGuiCylindricalSpace) {
      var cylindricalSpace = gui.space as LeapGuiCylindricalSpace;

      if (_cylindrical_worldToAnchor.Count != gui.elements.Count) {
        _cylindrical_worldToAnchor.Fill(gui.elements.Count, Matrix4x4.identity);
      }

      if (_cylindrical_elementParameters.Count != gui.elements.Count) {
        _cylindrical_elementParameters.Fill(gui.elements.Count, Vector4.zero);
      }

      if (_cylindrical_meshTransforms.Count != gui.elements.Count) {
        _cylindrical_meshTransforms.Fill(gui.elements.Count, Matrix4x4.identity);
      }

      using (new ProfilerSample("Assemble Data")) {
        for (int i = 0; i < _meshes.Count; i++) {
          var element = gui.elements[i];
          var parameters = cylindricalSpace.GetElementParameters(element.anchor, element.transform.position);
          var pos = cylindricalSpace.TransformPoint(element, gui.transform.InverseTransformPoint(element.transform.position));

          Quaternion rot = Quaternion.Euler(0, parameters.angleOffset * Mathf.Rad2Deg, 0);

          Matrix4x4 guiMesh = transform.localToWorldMatrix * Matrix4x4.TRS(pos, rot, Vector3.one);
          Matrix4x4 deform = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position - element.transform.position, Quaternion.identity, Vector3.one) * element.transform.localToWorldMatrix;
          Matrix4x4 total = guiMesh * deform;

          _cylindrical_elementParameters[i] = parameters;
          _cylindrical_meshTransforms[i] = total;
          _cylindrical_worldToAnchor[i] = guiMesh.inverse;
        }
      }

      using (new ProfilerSample("Update Material")) {
        _material.SetFloat(LeapGuiCylindricalSpace.RADIUS_PROPERTY, cylindricalSpace.radius);
        _material.SetMatrixArray("_LeapGuiCylindrical_WorldToAnchor", _cylindrical_worldToAnchor);
        _material.SetMatrix("_LeapGui_LocalToWorld", transform.localToWorldMatrix);
        _material.SetVectorArray("_LeapGuiCylindrical_ElementParameters", _cylindrical_elementParameters);
      }

      using (new ProfilerSample("Draw Meshes")) {
        for (int i = 0; i < _meshes.Count; i++) {
          Graphics.DrawMesh(_meshes[i], _cylindrical_meshTransforms[i], _material, 0);
        }
      }
    } else if (gui.space is LeapGuiSphericalSpace) {
      var sphericalSpace = gui.space as LeapGuiSphericalSpace;

      if (_spherical_worldToAnchor.Count != gui.elements.Count) {
        _spherical_worldToAnchor.Fill(gui.elements.Count, Matrix4x4.identity);
      }

      if (_spherical_elementParameters.Count != gui.elements.Count) {
        _spherical_elementParameters.Fill(gui.elements.Count, Vector4.zero);
      }

      if (_spherical_meshTransforms.Count != gui.elements.Count) {
        _spherical_meshTransforms.Fill(gui.elements.Count, Matrix4x4.identity);
      }

      for (int i = 0; i < _meshes.Count; i++) {
        var element = gui.elements[i];
        var parameters = sphericalSpace.GetElementParameters(element.anchor, element.transform.position);
        var pos = sphericalSpace.TransformPoint(element, gui.transform.InverseTransformPoint(element.transform.position));

        Quaternion rot = Quaternion.Euler(-parameters.angleYOffset * Mathf.Rad2Deg,
                                          parameters.angleXOffset * Mathf.Rad2Deg,
                                          0);

        Matrix4x4 guiMesh = transform.localToWorldMatrix * Matrix4x4.TRS(pos, rot, Vector3.one);
        Matrix4x4 deform = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position - element.transform.position, Quaternion.identity, Vector3.one) * element.transform.localToWorldMatrix;
        Matrix4x4 total = guiMesh * deform;

        _spherical_elementParameters[i] = parameters;
        _spherical_meshTransforms[i] = total;
        _spherical_worldToAnchor[i] = guiMesh.inverse;
      }

      _material.SetFloat(LeapGuiSphericalSpace.RADIUS_PROPERTY, sphericalSpace.radius);
      _material.SetMatrixArray("_LeapGuiSpherical_WorldToAnchor", _spherical_worldToAnchor);
      _material.SetMatrix("_LeapGui_LocalToWorld", transform.localToWorldMatrix);
      _material.SetVectorArray("_LeapGuiSpherical_ElementParameters", _spherical_elementParameters);

      for (int i = 0; i < _meshes.Count; i++) {
        Graphics.DrawMesh(_meshes[i], _spherical_meshTransforms[i], _material, 0);
      }
    }
  }

  protected override void doGenerationSetup() {
    if (_shader == null) {
      _shader = Shader.Find("LeapGui/Defaults/Dynamic");
    }

    base.doGenerationSetup();

    if (gui.space is LeapGuiCylindricalSpace) {
      _material.EnableKeyword(LeapGuiCylindricalSpace.FEATURE_NAME);
    } else if (gui.space is LeapGuiSphericalSpace) {
      _material.EnableKeyword(LeapGuiSphericalSpace.FEATURE_NAME);
    }
  }

  protected override void buildElement() {
    //Always start a new mesh for each element
    finishMesh();
    beginMesh();

    base.buildElement();
  }

  protected override Vector3 elementVertToMeshVert(Vector3 vertex) {
    return vertex;
  }
}
