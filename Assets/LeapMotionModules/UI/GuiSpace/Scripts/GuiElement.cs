using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity;
using Leap.Unity.Attributes;

public class GuiElement : MonoBehaviour {

  [Disable]
  [AutoFind(AutoFindLocations.Parents)]
  [SerializeField]
  private GuiMeshBaker _baker;

  [Tooltip("Mesh to display for this gui element.  Is must have a topology of " +
           "triangles, and have only a single submesh.")]
  [SerializeField]
  private Mesh _mesh;

  //Don't serialize references to the textures directly, or else unity will include
  //them in the build!  We don't want them in the build because they are being baked
  //into an atlas that will be included instead.
  [SerializeField]
  private string[] textureGUIDs;

  [Tooltip("If the mesh has no vertex colors, use this color as a vertex color. " +
           "If the mesh does have vertex colors, tint them with this color. " +
           "This property cannot be changed at runtime, use tints instead.")]
  [SerializeField]
  private Color _vertexColor;

  [SerializeField]
  private Color tint;

  [SerializeField]
  private GuiBlendShape _blendShape;

  public Mesh mesh {
    get {
      return _mesh;
    }
    set {
      _mesh = value;
    }
  }

#if UNITY_EDITOR
  public Texture2D GetTexture(int channel) {
    if (channel < 0 || channel >= textureGUIDs.Length) {
      return null;
    }

    string path = AssetDatabase.GUIDToAssetPath(textureGUIDs[channel]);
    if (string.IsNullOrEmpty(path)) {
      return null;
    }

    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
#endif

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




  void OnDrawGizmos() {
    Gizmos.matrix = transform.localToWorldMatrix;
    Gizmos.color = new Color(0, 0, 0, 0);
    Gizmos.DrawMesh(_mesh);
  }

  public struct ColorChannel {
    public string name;
    public Color color;
  }

  public struct FloatChannels {
    public string name;
    public float value;
  }

  public struct VectorChannels {
    public string name;
    public Vector4 vector;
  }

  public struct TextureChannels {
    public string name;
    public Sprite sprite;
  }
}
