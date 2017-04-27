using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Space;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [AddComponentMenu("")]
  [LeapGraphicTag("Baked")]
  public class LeapBakedRenderer : LeapMesherBase {

    #region INSPECTOR FIELDS

    [SerializeField]
    private SingleLayer _layer = 0;

    [EditTimeOnly]
    [SerializeField]
    private MotionType _motionType = MotionType.Translation;

    [EditTimeOnly]
    [SerializeField]
    private bool _createMeshRenderers;
    #endregion

    #region PRIVATE VARIABLES

    [SerializeField, HideInInspector]
    private List<MeshRendererContainer> _renderers;

    //## Rect space
    private const string RECT_POSITIONS = LeapGraphicRenderer.PROPERTY_PREFIX + "Rect_GraphicPositions";
    private List<Vector4> _rect_graphicPositions = new List<Vector4>();

    //## Cylindrical/Spherical spaces
    private const string CURVED_PARAMETERS = LeapGraphicRenderer.PROPERTY_PREFIX + "Curved_GraphicParameters";
    private List<Vector4> _curved_graphicParameters = new List<Vector4>();

    //## Cache data to be used inside of graphicVertToMeshVert
    private Matrix4x4 _translation_graphicVertToMeshVert;
    private Matrix4x4 _noMotion_graphicVertToLocalVert;
    private ITransformer _noMotion_transformer;

    #endregion

    public enum MotionType {
      None,
      Translation
    }

    public override SupportInfo GetSpaceSupportInfo(LeapSpace space) {
      if (space == null ||
          space is LeapCylindricalSpace ||
          space is LeapSphericalSpace) {
        return SupportInfo.FullSupport();
      } else {
        return SupportInfo.Error("Baked Renderer does not support " + space.GetType().Name);
      }
    }

    public override void OnUpdateRenderer() {
      base.OnUpdateRenderer();

      if (_motionType != MotionType.None) {
        if (renderer.space == null) {
          using (new ProfilerSample("Build Material Data")) {
            _rect_graphicPositions.Clear();
            foreach (var graphic in group.graphics) {
              var localSpace = transform.InverseTransformPoint(graphic.transform.position);
              _rect_graphicPositions.Add(localSpace);
            }
          }

          using (new ProfilerSample("Upload Material Data")) {
            _material.SetVectorArraySafe(RECT_POSITIONS, _rect_graphicPositions);
          }
        } else if (renderer.space is LeapRadialSpace) {
          var radialSpace = renderer.space as LeapRadialSpace;

          using (new ProfilerSample("Build Material Data")) {
            _curved_graphicParameters.Clear();
            foreach (var graphic in group.graphics) {
              var t = graphic.anchor.transformer as IRadialTransformer;
              _curved_graphicParameters.Add(t.GetVectorRepresentation(graphic.transform));
            }
          }

          using (new ProfilerSample("Upload Material Data")) {
            _material.SetFloat(SpaceProperties.RADIAL_SPACE_RADIUS, radialSpace.radius);
            _material.SetVectorArraySafe(CURVED_PARAMETERS, _curved_graphicParameters);
          }
        }
      }

      if (!_createMeshRenderers) {
        using (new ProfilerSample("Draw Meshes")) {
          for (int i = 0; i < _meshes.Count; i++) {
            Mesh mesh = _meshes[i];
            if (mesh != null) {
              Graphics.DrawMesh(_meshes[i], renderer.transform.localToWorldMatrix, _material, _layer);
            }
          }
        }
      }
    }

#if UNITY_EDITOR
    public override void OnDisableRendererEditor() {
      base.OnDisableRendererEditor();

      foreach (var renderer in _renderers) {
        renderer.Destroy();
      }
      _renderers.Clear();
    }

    public override void OnUpdateRendererEditor(bool isHeavyUpdate) {
      base.OnUpdateRendererEditor(isHeavyUpdate);

      if (_renderers == null) {
        _renderers = new List<MeshRendererContainer>();
      }

      if (_createMeshRenderers) {
        while (_renderers.Count > _meshes.Count) {
          _renderers.RemoveLast().Destroy();
        }

        while (_renderers.Count < _meshes.Count) {
          _renderers.Add(new MeshRendererContainer(transform));
        }

        for (int i = 0; i < _meshes.Count; i++) {
          _renderers[i].MakeValid(transform, i, _meshes[i], _material);
        }
      } else {
        while (_renderers.Count > 0) {
          _renderers.RemoveLast().Destroy();
        }
      }
    }
#endif

    protected override void setupForBuilding() {
      if (_shader == null) {
        _shader = Shader.Find("Leap Motion/Graphic Renderer/Unlit/Baked");
      }

      base.setupForBuilding();

      switch (_motionType) {
        case MotionType.Translation:
          _material.EnableKeyword(LeapGraphicRenderer.FEATURE_MOVEMENT_TRANSLATION);
          break;
      }

      if (_motionType != MotionType.None) {
        if (renderer.space is LeapCylindricalSpace) {
          _material.EnableKeyword(SpaceProperties.CYLINDRICAL_FEATURE);
        } else if (renderer.space is LeapSphericalSpace) {
          _material.EnableKeyword(SpaceProperties.SPHERICAL_FEATURE);
        }
      }
    }

    protected override void buildTopology() {
      //If the next graphic is going to put us over the limit, finish the current mesh
      //and start a new one.
      if (_generation.verts.Count + _generation.graphic.mesh.vertexCount > MeshUtil.MAX_VERT_COUNT) {
        finishMesh();
        beginMesh();
      }

      switch (_motionType) {
        case MotionType.None:
          _noMotion_transformer = _generation.graphic.transformer;
          _noMotion_graphicVertToLocalVert = renderer.transform.worldToLocalMatrix *
                                             _generation.graphic.transform.localToWorldMatrix;
          break;
        case MotionType.Translation:
          _translation_graphicVertToMeshVert = Matrix4x4.TRS(-renderer.transform.InverseTransformPoint(_generation.graphic.transform.position),
                                                             Quaternion.identity,
                                                             Vector3.one) *
                                               renderer.transform.worldToLocalMatrix *
                                               _generation.graphic.transform.localToWorldMatrix;

          break;
        default:
          throw new NotImplementedException();
      }

      base.buildTopology();
    }

    protected override void postProcessMesh() {
      base.postProcessMesh();

      //For the baked renderer, the mesh really never accurately represents it's visual position 
      //or size, so just disable culling entirely by making the bound gigantic.
      _generation.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
    }

    protected override bool doesRequireSpecialUv3() {
      if (_motionType != MotionType.None) {
        return true;
      }

      return base.doesRequireSpecialUv3();
    }

    protected override Vector3 graphicVertToMeshVert(Vector3 vertex) {
      switch (_motionType) {
        case MotionType.None:
          var localVert = _noMotion_graphicVertToLocalVert.MultiplyPoint3x4(vertex);
          return _noMotion_transformer.TransformPoint(localVert);
        case MotionType.Translation:
          return _translation_graphicVertToMeshVert.MultiplyPoint3x4(vertex);
      }

      throw new NotImplementedException();
    }

    protected override void graphicVertNormalToMeshVertNormal(Vector3 vertex,
                                                              Vector3 normal,
                                                          out Vector3 meshVert,
                                                          out Vector3 meshNormal) {
      switch (_motionType) {
        case MotionType.None:
          var localVert = _noMotion_graphicVertToLocalVert.MultiplyPoint3x4(vertex);
          var localNormal = _noMotion_graphicVertToLocalVert.MultiplyVector(normal);
          var matrix = _noMotion_transformer.GetTransformationMatrix(localVert);
          meshVert = matrix.MultiplyPoint3x4(Vector3.zero);
          meshNormal = matrix.MultiplyVector(localNormal);
          return;
        case MotionType.Translation:
          meshVert = _translation_graphicVertToMeshVert.MultiplyPoint3x4(vertex);
          meshNormal = _translation_graphicVertToMeshVert.MultiplyVector(normal);
          return;
      }

      throw new NotImplementedException();
    }

    [Serializable]
    protected class MeshRendererContainer {
      public GameObject obj;
      public MeshFilter filter;
      public MeshRenderer renderer;

      public MeshRendererContainer(Transform root) {
        obj = new GameObject("Graphic Renderer");
        filter = null;
        renderer = null;
      }

      public void Destroy() {
        DestroyImmediate(obj);
      }

      public void MakeValid(Transform root, int index, Mesh mesh, Material material) {
        obj.transform.SetParent(root);
        obj.transform.SetSiblingIndex(index);
        obj.SetActive(true);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        if (filter == null) {
          filter = InternalUtility.AddComponent<MeshFilter>(obj);
        }
        filter.sharedMesh = mesh;

        if (renderer == null) {
          renderer = InternalUtility.AddComponent<MeshRenderer>(obj);
        }
        renderer.enabled = true;
        renderer.sharedMaterial = material;
      }
    }
  }
}
