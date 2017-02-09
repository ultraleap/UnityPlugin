using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Baked")]
public class LeapGuiBakedRenderer : LeapGuiMesherBase {

  #region INSPECTOR FIELDS

  [SerializeField]
  private LayerMask _layer;

  [SerializeField]
  private MotionType _motionType = MotionType.Translation;
  #endregion

  #region PRIVATE VARIABLES

  //## Rect space
  private const string RECT_POSITIONS = LeapGui.PROPERTY_PREFIX + "Rect_ElementPositions";
  private List<Vector4> _rect_elementPositions = new List<Vector4>();

  //## Cylindrical space
  private const string CYLINDRICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Cylindrical_ElementParameters";
  private List<Vector4> _cylindrical_elementParameters = new List<Vector4>();

  //## Spherical space
  private const string SPHERICAL_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Spherical_ElementParameters";
  private List<Vector4> _spherical_elementParameters = new List<Vector4>();
  #endregion

  public enum MotionType {
    None,
    Translation,
    Full
  }

  public override SupportInfo GetSpaceSupportInfo(LeapGuiSpace space) {
    if (space is LeapGuiRectSpace ||
        space is LeapGuiCylindricalSpace) {
      return SupportInfo.FullSupport();
    } else {
      return SupportInfo.Error("Baked Renderer does not support " + space.GetType().Name);
    }
  }

  public override void OnUpdateRenderer() {
    base.OnUpdateRenderer();

    if (gui.space is LeapGuiRectSpace) {
      using (new ProfilerSample("Build Array")) {
        _rect_elementPositions.Clear();
        foreach (var element in gui.elements) {
          var guiSpace = transform.InverseTransformPoint(element.transform.position);
          _rect_elementPositions.Add(guiSpace);
        }
      }

      using (new ProfilerSample("Upload Array")) {
        _material.SetVectorArray(RECT_POSITIONS, _rect_elementPositions);
      }
    } else if (gui.space is LeapGuiCylindricalSpace) {
      var cylindricalSpace = gui.space as LeapGuiCylindricalSpace;

      _cylindrical_elementParameters.Clear();
      foreach (var element in gui.elements) {
        var parameters = cylindricalSpace.GetElementParameters(element.anchor, element.transform.position);
        _cylindrical_elementParameters.Add(parameters);
      }

      _material.SetFloat(LeapGuiCylindricalSpace.RADIUS_PROPERTY, cylindricalSpace.radius);
      _material.SetVectorArray(CYLINDRICAL_PARAMETERS, _cylindrical_elementParameters);
    } else if (gui.space is LeapGuiSphericalSpace) {
      var sphericalSpace = gui.space as LeapGuiSphericalSpace;

      _spherical_elementParameters.Clear();
      foreach (var element in gui.elements) {
        var parameters = sphericalSpace.GetElementParameters(element.anchor, element.transform.position);
        _spherical_elementParameters.Add(parameters);
      }

      _material.SetFloat(LeapGuiSphericalSpace.RADIUS_PROPERTY, sphericalSpace.radius);
      _material.SetVectorArray(SPHERICAL_PARAMETERS, _spherical_elementParameters);
    }

    using (new ProfilerSample("Draw Meshes")) {
      foreach (var mesh in _meshes) {
        Graphics.DrawMesh(mesh, gui.transform.localToWorldMatrix, _material, 0);
      }
    }
  }

  protected override void doGenerationSetup() {
    if (_shader == null) {
      _shader = Shader.Find("LeapGui/Defaults/Baked");
    }

    base.doGenerationSetup();

    switch (_motionType) {
      case MotionType.Translation:
        _material.EnableKeyword(LeapGui.FEATURE_MOVEMENT_TRANSLATION);
        break;
      case MotionType.Full:
        _material.EnableKeyword(LeapGui.FEATURE_MOVEMENT_FULL);
        break;
    }

    if (gui.space is LeapGuiCylindricalSpace) {
      _material.EnableKeyword(LeapGuiCylindricalSpace.FEATURE_NAME);
    } else if (gui.space is LeapGuiSphericalSpace) {
      _material.EnableKeyword(LeapGuiSphericalSpace.FEATURE_NAME);
    }
  }

  protected override void buildTopology(LeapGuiMeshData meshData) {
    if (_verts.Count + meshData.mesh.vertexCount > MeshUtil.MAX_VERT_COUNT) {
      finishMesh();
      beginMesh();
    }

    base.buildTopology(meshData);
  }

  protected override void postProcessMesh() {
    base.postProcessMesh();

    _currMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
  }

  protected override Vector3 elementVertToMeshVert(Vector3 vertex) {
    switch (_motionType) {
      case MotionType.None:
        return gui.space.TransformPoint(vertex);
      case MotionType.Translation:
        Vector3 worldVert = _currElement.transform.TransformPoint(vertex);
        Vector3 guiVert = gui.transform.InverseTransformPoint(worldVert);
        return guiVert - gui.transform.InverseTransformPoint(_currElement.transform.position);
    }

    throw new NotImplementedException();
  }
}
