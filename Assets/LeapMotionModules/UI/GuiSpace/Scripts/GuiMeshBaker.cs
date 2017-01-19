using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Attributes;
using Leap.Unity.Query;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GuiMeshBaker : MonoBehaviour {

  [Header("Mesh Options")]
  [Tooltip("Enabling this will cause the UV0 channel to contain coordinates for displaying " +
           "the textures stored in Gui Elements.  The textures will be packed into an atlas " +
           "so that the gui can be drawn with a single draw call.")]
  [SerializeField]
  private bool enableTextures;

  [SerializeField]
  private AtlasSettings atlasSettings;

  [Tooltip("Enabling this will cause the vertex colors of each gui element to be baked into " +
           "the final gui mesh.")]
  [SerializeField]
  private bool enableVertexColors;

  [Tooltip("Tint the baked vertex colors with this color.")]
  [SerializeField]
  private Color bakedTint;

  /// <summary>
  /// Enabling any of these options will cause UV3's to be baked into the mesh.  The W component
  /// of this vector will be used as an element index, which is used by all of these effects.
  /// In the case that blend shape animation is turned on, the XYX components will be used 
  /// to define that space.
  /// </summary>
  [Header("Animation Options")]
  [Tooltip("Allows gui elements to move around freely.")]
  [SerializeField]
  private bool enableElementMotion = false;

  [Tooltip("Allows each gui element to move, instead of being stationary.  Also allows " +
           "the properties of the gui space to be changed at runtime.")]
  [SerializeField]
  private MotionType motionType = MotionType.TranslationOnly;

  [Tooltip("Allows each gui element to recieve a different tint color at runtime.")]
  [SerializeField]
  private bool enableTinting = false;

  [Tooltip("Allows each gui element to warp to and from a different shape at runtime.  " +
           "Shape data is baked out as a position delta into UV3.")]
  [SerializeField]
  private bool enableBlendShapes = false;

  [Tooltip("Defines what coordinate space blend shapes are defined in.")]
  [SerializeField]
  private BlendShapeSpace blendShapeSpace = BlendShapeSpace.Local;

  /// <summary>
  /// The basic shader does not support any custom animation by default.  If you want
  /// custom animation for your gui, you will need to create new shaders to handle
  /// the additional animation data.
  /// </summary>
  [Header("Custom Animation")]
  [SerializeField]
  private bool enableCustomChannels = false;

  [SerializeField]
  private ChannelDef[] customChannels;

  void Update() {
    //Bake();
    //Graphics.DrawMesh(bakedMesh, Matrix4x4.identity, material, 0);
  }

  /*
  public void Bake() {
    var elements = GetComponentsInChildren<GuiElement>();

    List<Vector3> verts = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<int> indexes = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();

    foreach (var element in elements) {
      var elementMesh = element.mesh;
      var elementTransform = element.transform;

      elementMesh.GetIndices(0).Query().Select(i => i + verts.Count).FillList(indexes);
      elementMesh.vertices.Query().Select(v => elementTransform.TransformPoint(v)).FillList(verts);

      normals.AddRange(elementMesh.normals);
      uvs.AddRange(elementMesh.uv);
    }

    if (bakedMesh == null) {
      bakedMesh = new Mesh();
      bakedMesh.hideFlags = HideFlags.HideAndDontSave;
    } else {
      bakedMesh.Clear();
    }

    bakedMesh.SetVertices(verts);
    bakedMesh.SetTriangles(indexes, 0);
    bakedMesh.SetNormals(normals);
    bakedMesh.SetUVs(0, uvs);

    colors.AddRange(uvs.Query().Select(u => new Color(2.8E-45f, 0.5f, 1)).ToList());
    bakedMesh.SetColors(colors);
  }
  */

  public enum MotionType {
    TranslationOnly,
    Full
  }

  public enum TintChannels {
    One,
    Two
  }

  public enum BlendShapeChannels {
    One,
    Two,
    Three
  }

  public enum ChannelType {
    Float,
    Vector,
    Color,
    Matrix,
    Texture
  }

  [Serializable]
  public struct ChannelDef {
    public string name;
    public ChannelType type;
  }

  public class ColorChannel {
    public string name;
    public List<Color> colors;
  }

  public class TransformChannel {
    public string name;
    public List<Matrix4x4> transforms;
  }

  public class VectorChannel {
    public string name;
    public List<Vector4> vectors;
  }

  public enum BlendShapeSpace {
    Local,
    World
  }

  [Serializable]
  public class AtlasSettings {
    [MinValue(0)]
    public int padding;

    [MinValue(32)]
    [MaxValue(4096)]
    public int maximumAtlasSize = 2048;

    public FilterMode filterMode = FilterMode.Bilinear;

    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
  }

}
