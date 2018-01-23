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
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Space;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Baked")]
  [Serializable]
  public class LeapBakedRenderer : LeapMesherBase {

    public const string DEFAULT_SHADER = "LeapMotion/GraphicRenderer/Unlit/Baked";

    #region INSPECTOR FIELDS

    [Tooltip("What type of graphic motion should be supported by this renderer?  Currently there are only two modes, None, and Translation.")]
    [EditTimeOnly]
    [SerializeField]
    private MotionType _motionType = MotionType.Translation;

    [Tooltip("Should the baked renderer create an actual game object and attach a mesh renderer to it in order to display the graphics?")]
    [EditTimeOnly]
    [SerializeField]
    private bool _createMeshRenderers;
    #endregion

    #region PRIVATE VARIABLES

    [SerializeField, HideInInspector]
    private List<MeshRendererContainer> _renderers = new List<MeshRendererContainer>();

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

    public override void OnEnableRenderer() {
      base.OnEnableRenderer();

      foreach (var renderer in _renderers) {
        renderer.ClearPropertyBlock();
      }
    }

    public override void OnUpdateRenderer() {
      using (new ProfilerSample("Update Baked Renderer")) {
        base.OnUpdateRenderer();

        if (_motionType != MotionType.None) {
          if (renderer.space == null) {
            using (new ProfilerSample("Build Material Data")) {
              _rect_graphicPositions.Clear();
              foreach (var graphic in group.graphics) {
                Vector3 localSpace;
                if (graphic.isActiveAndEnabled) {
                  localSpace = renderer.transform.InverseTransformPoint(graphic.transform.position);
                } else {
                  localSpace = Vector3.one * float.NaN;
                }

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
                Vector4 vectorRepresentation;
                if (graphic.isActiveAndEnabled) {
                  vectorRepresentation = t.GetVectorRepresentation(graphic.transform);
                } else {
                  vectorRepresentation = Vector4.one * float.NaN;
                }

                _curved_graphicParameters.Add(vectorRepresentation);
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
              drawMesh(_meshes[i], renderer.transform.localToWorldMatrix);
            }
          }
        }
      }
    }

#if UNITY_EDITOR
    public override void OnEnableRendererEditor() {
      base.OnEnableRendererEditor();

      _shader = Shader.Find(DEFAULT_SHADER);
    }

    public override void OnDisableRendererEditor() {
      base.OnDisableRendererEditor();

      foreach (var renderer in _renderers) {
        renderer.Destroy();
      }
      _renderers.Clear();
    }

    public override void OnUpdateRendererEditor() {
      base.OnUpdateRendererEditor();

      if (_renderers == null) {
        _renderers = new List<MeshRendererContainer>();
      }

      if (_createMeshRenderers) {
        for (int i = _renderers.Count; i-- != 0;) {
          if (_renderers[i].obj == null) {
            _renderers.RemoveAt(i);
          }
        }

        while (_renderers.Count > _meshes.Count) {
          _renderers.RemoveLast().Destroy();
        }

        while (_renderers.Count < _meshes.Count) {
          _renderers.Add(new MeshRendererContainer(renderer.transform));
        }

        for (int i = 0; i < _meshes.Count; i++) {
          _renderers[i].MakeValid(renderer.transform, i, _meshes[i], _material, _spriteTextureBlock);
        }
      } else {
        while (_renderers.Count > 0) {
          _renderers.RemoveLast().Destroy();
        }
      }
    }
#endif

    protected override void prepareMaterial() {
      if (_shader == null) {
        _shader = Shader.Find(DEFAULT_SHADER);
      }

      base.prepareMaterial();

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
        finishAndAddMesh();
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

    protected override bool doesRequireVertInfo() {
      if (_motionType != MotionType.None) {
        return true;
      }

      return base.doesRequireVertInfo();
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
#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(obj, "Created graphic renderer");
#endif

        filter = null;
        renderer = null;
      }

      public void Destroy() {
#if UNITY_EDITOR
        if (!Application.isPlaying) {
          Undo.DestroyObjectImmediate(obj);
        } else
#endif
        {
          UnityEngine.Object.Destroy(obj);
        }
      }

      public void MakeValid(Transform root, int index, Mesh mesh, Material material, MaterialPropertyBlock block) {
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
        renderer.SetPropertyBlock(block);
      }

      public void ClearPropertyBlock() {
        renderer.SetPropertyBlock(new MaterialPropertyBlock());
      }
    }
  }
}
