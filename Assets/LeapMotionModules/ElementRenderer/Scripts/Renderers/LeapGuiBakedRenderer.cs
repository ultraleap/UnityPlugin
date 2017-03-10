using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

[AddComponentMenu("")]
[LeapGuiTag("Baked")]
public class LeapGuiBakedRenderer : LeapGuiMesherBase {

  #region INSPECTOR FIELDS

  [EditTimeOnly]
  [SerializeField]
  private SingleLayer _layer = 0;

  [EditTimeOnly]
  [SerializeField]
  private MotionType _motionType = MotionType.Translation;

  [EditTimeOnly]
  [SerializeField]
  private bool _createMeshRenderers;

  [EditTimeOnly]
  [SerializeField]
  private bool _enableLightmapping;

  [EditTimeOnly]
  [SerializeField]
  private MaterialGlobalIlluminationFlags _giFlags = MaterialGlobalIlluminationFlags.None;

  [EditTimeOnly]
  [SerializeField]
  private LightmapUnwrapSettings _lightmapUnwrapSettings;
  #endregion

  #region PRIVATE VARIABLES

  [SerializeField, HideInInspector]
  private List<MeshRendererContainer> _renderers;

  //## Rect space
  private const string RECT_POSITIONS = LeapGui.PROPERTY_PREFIX + "Rect_ElementPositions";
  private List<Vector4> _rect_elementPositions = new List<Vector4>();

  //## Cylindrical/Spherical spaces
  private const string CURVED_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Curved_ElementParameters";
  private List<Vector4> _curved_elementParameters = new List<Vector4>();

  //## Cache data to be used inside of elementVertToMeshVert
  private Matrix4x4 _translation_elementVertToMeshVert;
  private Matrix4x4 _noMotion_elementVertToGuiVert;
  private ITransformer _noMotion_transformer;

  #endregion

  public enum MotionType {
    None,
    Translation
  }

  protected override void OnValidate() {
    base.OnValidate();

    if (!_createMeshRenderers) {
      _enableLightmapping = false;
    }
  }

  public override SupportInfo GetSpaceSupportInfo(LeapGuiSpace space) {
    if (space is LeapGuiRectSpace ||
        space is LeapGuiCylindricalSpace) {
      return SupportInfo.FullSupport();
    } else {
      return SupportInfo.Error("Baked Renderer does not support " + space.GetType().Name);
    }
  }

  public override void OnDisableRendererEditor() {
    base.OnDisableRendererEditor();

    foreach (var renderer in _renderers) {
      renderer.Destroy();
    }
    _renderers.Clear();
  }

  public override void OnUpdateRenderer() {
    base.OnUpdateRenderer();

    if (gui.space is LeapGuiRectSpace) {
      using (new ProfilerSample("Build Material Data")) {
        _rect_elementPositions.Clear();
        foreach (var element in group.elements) {
          var guiSpace = transform.InverseTransformPoint(element.transform.position);
          _rect_elementPositions.Add(guiSpace);
        }
      }

      using (new ProfilerSample("Upload Material Data")) {
        _material.SetVectorArraySafe(RECT_POSITIONS, _rect_elementPositions);
      }
    } else if (gui.space is LeapGuiRadialSpaceBase) {
      var radialSpace = gui.space as LeapGuiRadialSpaceBase;

      using (new ProfilerSample("Build Material Data")) {
        _curved_elementParameters.Clear();
        foreach (var element in group.elements) {
          var t = radialSpace.GetTransformer(element.anchor) as IRadialTransformer;
          _curved_elementParameters.Add(t.GetVectorRepresentation(element));
        }
      }

      using (new ProfilerSample("Upload Material Data")) {
        _material.SetFloat(LeapGuiRadialSpaceBase.RADIUS_PROPERTY, radialSpace.radius);
        _material.SetVectorArraySafe(CURVED_PARAMETERS, _curved_elementParameters);
      }
    }

    if (!_createMeshRenderers) {
      using (new ProfilerSample("Draw Meshes")) {
        foreach (var mesh in _meshes) {
          Graphics.DrawMesh(mesh, gui.transform.localToWorldMatrix, _material, _layer);
        }
      }
    }
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
        if (_enableLightmapping) {
          GameObjectUtility.SetStaticEditorFlags(_renderers[i].obj, StaticEditorFlags.LightmapStatic | StaticEditorFlags.ReflectionProbeStatic);
        }
      }
    } else {
      while (_renderers.Count > 0) {
        _renderers.RemoveLast().Destroy();
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

  protected override void prepareMaterial() {
    base.prepareMaterial();

    if (_enableLightmapping) {
      _material.globalIlluminationFlags = _giFlags;
    }
  }

  protected override void buildTopology() {
    //If the next element is going to put us over the limit, finish the current mesh
    //and start a new one.
    if (_verts.Count + _currElement.mesh.vertexCount > MeshUtil.MAX_VERT_COUNT) {
      finishMesh();
      beginMesh();
    }

    switch (_motionType) {
      case MotionType.None:
        _noMotion_transformer = gui.space.GetTransformer(_currElement.anchor);
        _noMotion_elementVertToGuiVert = gui.transform.worldToLocalMatrix *
                                         _currElement.transform.localToWorldMatrix;
        break;
      case MotionType.Translation:
        _translation_elementVertToMeshVert = Matrix4x4.TRS(-gui.transform.InverseTransformPoint(_currElement.transform.position),
                                                           Quaternion.identity,
                                                           Vector3.one) *
                                             gui.transform.worldToLocalMatrix *
                                             _currElement.transform.localToWorldMatrix;

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
    _currMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);

    //No solution right now for baking lightmap uvs at runtime
    //But, that wasn't important anyway :P
#if UNITY_EDITOR
    if (_enableLightmapping && isHeavyUpdate) {
      _lightmapUnwrapSettings.GenerateLightmapUvs(_currMesh);
    }
#endif
  }

  protected override bool doesRequireSpecialUv3() {
    if (_motionType != MotionType.None) {
      return true;
    }

    return base.doesRequireSpecialUv3();
  }

  protected override Vector3 elementVertToMeshVert(Vector3 vertex) {
    switch (_motionType) {
      case MotionType.None:
        var guiVert = _noMotion_elementVertToGuiVert.MultiplyPoint3x4(vertex);
        return _noMotion_transformer.TransformPoint(guiVert);
      case MotionType.Translation:
        return _translation_elementVertToMeshVert.MultiplyPoint3x4(vertex);
    }

    throw new NotImplementedException();
  }

  protected override void elementVertNormalToMeshVertNormal(Vector3 vertex,
                                                            Vector3 normal,
                                                        out Vector3 meshVert,
                                                        out Vector3 meshNormal) {
    switch (_motionType) {
      case MotionType.None:
        var guiVert = _noMotion_elementVertToGuiVert.MultiplyPoint3x4(vertex);
        var guiNormal = _noMotion_elementVertToGuiVert.MultiplyVector(normal);
        var matrix = _noMotion_transformer.GetTransformationMatrix(guiVert);
        meshVert = matrix.MultiplyPoint3x4(Vector3.zero);
        meshNormal = matrix.MultiplyVector(guiNormal);
        return;
      case MotionType.Translation:
        meshVert = _translation_elementVertToMeshVert.MultiplyPoint3x4(vertex);
        meshNormal = _translation_elementVertToMeshVert.MultiplyVector(normal);
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
        filter = obj.AddComponent<MeshFilter>();
      }
      filter.sharedMesh = mesh;

      if (renderer == null) {
        renderer = obj.AddComponent<MeshRenderer>();
      }
      renderer.enabled = true;
      renderer.sharedMaterial = material;
    }
  }
}
