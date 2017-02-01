using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity;
using Leap.Unity.Attributes;

namespace Leap.Unity.Gui.Space {

  [DisallowMultipleComponent]
  public class LeapElement : MonoBehaviour {

    [Disable]
    [AutoFind(AutoFindLocations.Parents)]
    [SerializeField]
    private GuiMeshBaker _baker;

    //For mesh and textures don't serialize references to the objects directly, or else 
    //unity will include them in the build!  We don't want them in the build because 
    //they are being baked into the final gui instead.

    [HideInInspector]
    [SerializeField]
    private NoReferenceMesh _mesh;

    [HideInInspector]
    [SerializeField]
    private Sprite[] _sprites;

    [Tooltip("If the mesh has no vertex colors, use this color as a vertex color. " +
             "If the mesh does have vertex colors, tint them with this color. " +
             "This property cannot be changed at runtime, use tints instead.")]
    [HideInInspector]
    [SerializeField]
    private Color _vertexColor = Color.white;

    [HideInInspector]
    [SerializeField]
    private Color _tint = Color.white;

    [HideInInspector]
    [SerializeField]
    private GuiBlendShape _blendShape;

    [HideInInspector]
    [Range(0, 1)]
    [SerializeField]
    private float _blendShapeAmount = 0;

    [HideInInspector]
    [SerializeField]
    private int _elementId;

    private ProceduralMeshSource _meshSource;

    public GuiMeshBaker baker {
      get {
        return _baker ?? GetComponentInParent<GuiMeshBaker>();
      }
    }

    public int elementId {
      get {
        return _elementId;
      }
      set {
        _elementId = value;
      }
    }

    public Color vertexColor {
      get {
        return _vertexColor;
      }
      set {
        _vertexColor = value;
      }
    }

    public GuiBlendShape blendShape {
      get {
        return _blendShape;
      }
    }

    public Color tint {
      get {
        return _tint;
      }
      set {
        _tint = value;
        _baker.SetTint(_elementId, _tint);
      }
    }

    public float blendShapeAmount {
      get {
        return _blendShapeAmount;
      }
      set {
        _blendShapeAmount = value;
        _baker.SetBlendShapeAmount(_elementId, _blendShapeAmount);
      }
    }

    public Mesh GetMesh() {
      //if (IsUsingProceduralMeshSource()) {
        //return _meshSource.GetMesh(this);
      //}// else {
        return _mesh.GetValue();
      //}
    }

    public bool DoesMeshHaveAtlasUvs(int uvChannel) {
      //if (IsUsingProceduralMeshSource()) {
      //  return _meshSource.DoesMeshHaveAtlasUvs(uvChannel);
      //} else {
      //  return false;//Assume user assigned meshes never have atlas coordinates
      //}
      return true;
    }

    public bool IsUsingProceduralMeshSource() {
      return false;
      //_meshSource = GetComponent<ProceduralMeshSource>();
      //return _meshSource != null && _meshSource.enabled && _meshSource.CanGenerateMeshForElement(this);
    }

    public Sprite GetSprite(int channel) {
      return _sprites[channel];
    }

    void OnDrawGizmos() {
      /*
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.color = new Color(0, 0, 0, 0);
      Gizmos.DrawMesh(_mesh);
       * */
    }
  }
}
