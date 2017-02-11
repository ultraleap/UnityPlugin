using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;

[AddComponentMenu("")]
[LeapGuiTag("Baked")]
public class LeapGuiBakedRenderer : LeapGuiMesherBase {

  #region INSPECTOR FIELDS

  [SerializeField]
  private SingleLayer _layer = 0;

  [SerializeField]
  private MotionType _motionType = MotionType.Translation;
  #endregion

  #region PRIVATE VARIABLES

  //## Rect space
  private const string RECT_POSITIONS = LeapGui.PROPERTY_PREFIX + "Rect_ElementPositions";
  private List<Vector4> _rect_elementPositions = new List<Vector4>();

  //## Cylindrical/Spherical spaces
  private const string CURVED_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Curved_ElementParameters";
  private List<Vector4> _curved_elementParameters = new List<Vector4>();

  #endregion

  public enum MotionType {
    None,
    Translation
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
      using (new ProfilerSample("Build Material Data")) {
        _rect_elementPositions.Clear();
        foreach (var element in gui.elements) {
          var guiSpace = transform.InverseTransformPoint(element.transform.position);
          _rect_elementPositions.Add(guiSpace);
        }
      }

      using (new ProfilerSample("Upload Material Data")) {
        _material.SetVectorArray(RECT_POSITIONS, _rect_elementPositions);
      }
    } else if (gui.space is LeapGuiRadialSpace) {
      var radialSpace = gui.space as LeapGuiRadialSpace;

      using (new ProfilerSample("Build Material Data")) {
        _curved_elementParameters.Clear();
        foreach (var element in gui.elements) {
          var t = radialSpace.GetTransformer(element.anchor) as LeapGuiRadialSpace.IRadialTransformer;
          _curved_elementParameters.Add(t.GetVectorRepresentation(element));
        }
      }

      using (new ProfilerSample("Upload Material Data")) {
        _material.SetFloat(LeapGuiRadialSpace.RADIUS_PROPERTY, radialSpace.radius);
        _material.SetVectorArray(CURVED_PARAMETERS, _curved_elementParameters);
      }
    }

    using (new ProfilerSample("Draw Meshes")) {
      foreach (var mesh in _meshes) {
        Graphics.DrawMesh(mesh, gui.transform.localToWorldMatrix, _material, _layer);
      }
    }
  }

  protected override void setupForBuilding() {
    if (_shader == null) {
      _shader = Shader.Find("LeapGui/Defaults/Baked");
    }

    base.setupForBuilding();

    switch (_motionType) {
      case MotionType.Translation:
        _material.EnableKeyword(LeapGui.FEATURE_MOVEMENT_TRANSLATION);
        break;
    }

    if (_motionType != MotionType.None) {
      if (gui.space is LeapGuiCylindricalSpace) {
        _material.EnableKeyword(LeapGuiCylindricalSpace.FEATURE_NAME);
      } else if (gui.space is LeapGuiSphericalSpace) {
        _material.EnableKeyword(LeapGuiSphericalSpace.FEATURE_NAME);
      }
    }
  }

  protected override void buildTopology(LeapGuiMeshData meshData) {
    //If the next element is going to put us over the limit, finish the current mesh
    //and start a new one.
    if (_verts.Count + meshData.mesh.vertexCount > MeshUtil.MAX_VERT_COUNT) {
      finishMesh();
      beginMesh();
    }

    base.buildTopology(meshData);
  }

  protected override void postProcessMesh() {
    base.postProcessMesh();

    //For the baked renderer, the mesh really never accurately represents it's visual position 
    //or size, so just disable culling entirely by making the bound gigantic.
    _currMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
  }

  protected override bool doesRequireSpecialUv3() {
    if (_motionType != MotionType.None) {
      return true;
    }

    return base.doesRequireSpecialUv3();
  }

  protected override Vector3 elementVertToMeshVert(Vector3 vertex) {
    Vector3 worldVert = _currElement.transform.TransformPoint(vertex);
    Vector3 guiVert = gui.transform.InverseTransformPoint(worldVert);
    switch (_motionType) {
      case MotionType.None:
        return gui.space.GetTransformer(_currElement.anchor).TransformPoint(guiVert);
      case MotionType.Translation:
        return guiVert - gui.transform.InverseTransformPoint(_currElement.transform.position);
    }

    throw new NotImplementedException();
  }
}
