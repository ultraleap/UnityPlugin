using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace Leap.Unity.RuntimeGizmos {

  /// <summary>
  /// Have your MonoBehaviour implement this interface to be able to draw runtime gizmos.
  /// Remember that you must use the RGizmos class, not Unity's Gizmos class, for runtime
  /// gizmos to work!  You must also have a RuntimeGizmoDrawer component in the scene.
  /// </summary>
  public interface IRuntimeGizmoDrawer {
    void OnDrawRuntimeGizmos();
  }

  public static class RGizmos {
    private static List<OperationType> _operations = new List<OperationType>();
    private static List<Matrix4x4> _matrices = new List<Matrix4x4>();
    private static List<Color> _colors = new List<Color>();
    private static List<Line> _lines = new List<Line>();
    private static List<Mesh> _meshes = new List<Mesh>();

    private static Color _currColor = Color.white;
    private static Matrix4x4 _currMatrix = Matrix4x4.identity;
    private static Stack<Matrix4x4> _matrixStack = new Stack<Matrix4x4>();

    private static bool _isInWireMode = false;

    public static Mesh cubeMesh, wireCubeMesh, sphereMesh, wireSphereMesh;

    /// <summary>
    /// Causes all remaining gizmos drawing to be done in the local coordinate space of the given transform.
    /// </summary>
    public static void RelativeTo(Transform transform) {
      matrix = transform.localToWorldMatrix;
    }

    /// <summary>
    /// Saves the current gizmo matrix to the gizmo matrix stack.
    /// </summary>
    public static void PushMatrix() {
      _matrixStack.Push(_currMatrix);
    }

    /// <summary>
    /// Restores the current gizmo matrix from the gizmo matrix stack.
    /// </summary>
    public static void PopMatrix() {
      matrix = _matrixStack.Pop();
    }

    /// <summary>
    /// Sets or gets the color for the gizmos that will be drawn next.
    /// </summary>
    public static Color color {
      get {
        return _currColor;
      }
      set {
        _currColor = value;
        _operations.Add(OperationType.SetColor);
        _colors.Add(_currColor);
      }
    }


    /// <summary>
    /// Sets or gets the matrix used to transform all gizmos.
    /// </summary>
    public static Matrix4x4 matrix {
      get {
        return _currMatrix;
      }
      set {
        _currMatrix = value;
        _operations.Add(OperationType.SetMatrix);
        _matrices.Add(_currMatrix);
      }
    }

    /// <summary>
    /// Draw a filled gizmo mesh using the given matrix transform.
    /// </summary>
    public static void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
      setWireMode(false);
      drawMeshInternal(mesh, matrix);
    }

    /// <summary>
    /// Draws a filled gizmo mesh at the given transform location.
    /// </summary>
    public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    /// <summary>
    /// Draws a wire gizmo mesh using the given matrix transform.
    /// </summary>
    public static void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
      setWireMode(true);
      drawMeshInternal(mesh, matrix);
    }

    /// <summary>
    /// Draws a wire gizmo mesh at the given transform location.
    /// </summary>
    public static void DrawWireMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawWireMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    /// <summary>
    /// Draws a gizmo line that connects the two positions.
    /// </summary>
    public static void DrawLine(Vector3 a, Vector3 b) {
      _operations.Add(OperationType.DrawLine);
      _lines.Add(new Line(a, b));
    }

    /// <summary>
    /// Draws a filled gizmo cube at the given position with the given size.
    /// </summary>
    public static void DrawCube(Vector3 position, Vector3 size) {
      DrawMesh(cubeMesh, position, Quaternion.identity, size);
    }

    /// <summary>
    /// Draws a wire gizmo cube at the given position with the given size.
    /// </summary>
    public static void DrawWireCube(Vector3 position, Vector3 size) {
      DrawWireMesh(wireCubeMesh, position, Quaternion.identity, size);
    }

    /// <summary>
    /// Draws a filled gizmo sphere at the given position with the given radius.
    /// </summary>
    public static void DrawSphere(Vector3 center, float radius) {
      DrawMesh(sphereMesh, center, Quaternion.identity, Vector3.one * radius * 2);
    }

    /// <summary>
    /// Draws a wire gizmo sphere at the given position with the given radius.
    /// </summary>
    public static void DrawWireSphere(Vector3 center, float radius) {
      DrawWireMesh(wireSphereMesh, center, Quaternion.identity, Vector3.one * radius * 2);
    }

    public static void ClearAllGizmos() {
      _operations.Clear();
      _matrices.Clear();
      _colors.Clear();
      _lines.Clear();
      _meshes.Clear();
      _isInWireMode = false;
      _currMatrix = Matrix4x4.identity;
      _currColor = Color.white;
    }

    private static Material _currMat;
    public static void DrawAllGizmosToScreen(Material wireMaterial, Material filledMaterial) {
      try {
        int matrixIndex = 0;
        int colorIndex = 0;
        int lineIndex = 0;
        int meshIndex = 0;

        _currMat = null;
        _currColor = Color.white;

        GL.wireframe = false;

        for (int i = 0; i < _operations.Count; i++) {
          OperationType type = _operations[i];
          switch (type) {
            case OperationType.SetMatrix:
              _currMatrix = _matrices[matrixIndex++];
              break;
            case OperationType.SetColor:
              _currColor = _colors[colorIndex++];
              _currMat = null;
              break;
            case OperationType.ToggleWireframe:
              GL.wireframe = !GL.wireframe;
              break;
            case OperationType.DrawLine:
              setMaterial(wireMaterial);

              GL.Begin(GL.LINES);
              Line line = _lines[lineIndex++];
              GL.Vertex(_currMatrix.MultiplyPoint3x4(line.a));
              GL.Vertex(_currMatrix.MultiplyPoint3x4(line.b));
              GL.End();
              break;
            case OperationType.DrawMesh:
              if (GL.wireframe) {
                setMaterial(wireMaterial);
              } else {
                setMaterial(filledMaterial);
              }

              Graphics.DrawMeshNow(_meshes[meshIndex++], _currMatrix * _matrices[matrixIndex++]);
              break;
            default:
              throw new InvalidOperationException("Unexpected operation type " + type);
          }
        }
      } finally {
        GL.wireframe = false;
      }
    }

    private static void setMaterial(Material mat) {
      if (_currMat != mat) {
        _currMat = mat;
        _currMat.color = _currColor;
        _currMat.SetPass(0);
      }
    }

    private static void drawMeshInternal(Mesh mesh, Matrix4x4 matrix) {
      _operations.Add(OperationType.DrawMesh);
      _meshes.Add(mesh);
      _matrices.Add(matrix);
    }

    private static void setWireMode(bool wireMode) {
      if (_isInWireMode != wireMode) {
        _operations.Add(OperationType.ToggleWireframe);
        _isInWireMode = wireMode;
      }
    }

    private enum OperationType {
      SetMatrix,
      ToggleWireframe,
      SetColor,
      DrawLine,
      DrawMesh
    }

    private struct Line {
      public Vector3 a, b;

      public Line(Vector3 a, Vector3 b) {
        this.a = a;
        this.b = b;
      }
    }
  }

  [ExecuteInEditMode]
  public class RuntimeGizmoDrawer : MonoBehaviour {
    public const int CIRCLE_RESOLUTION = 32;

    [Tooltip("Should the gizmos be visible in the game view.")]
    [SerializeField]
    private bool displayInGameView = true;

    [Tooltip("The mesh to use for the filled sphere gizmo.")]
    [SerializeField]
    private Mesh _sphereMesh;

    [Tooltip("The shader to use for rendering wire gizmos.")]
    [SerializeField]
    private Shader _wireShader;

    [Tooltip("The shader to use for rendering filled gizmos.")]
    [SerializeField]
    private Shader _filledShader;

    private Material _wireMaterial, _filledMaterial;
    private Mesh _cubeMesh, _wireCubeMesh, _wireSphereMesh;

    private void onPostRender(Camera camera) {
      bool isSceneCamera = camera.gameObject.hideFlags == HideFlags.HideAndDontSave;
      if (!isSceneCamera && !displayInGameView) {
        return;
      }

      RGizmos.DrawAllGizmosToScreen(_wireMaterial, _filledMaterial);
    }

    void OnEnable() {
      generateMeshes();

      Camera.onPostRender -= onPostRender;
      Camera.onPostRender += onPostRender;
    }

    void OnDisable() {
      Camera.onPostRender -= onPostRender;
    }

    private List<GameObject> _objList = new List<GameObject>();
    private List<IRuntimeGizmoDrawer> _gizmoList = new List<IRuntimeGizmoDrawer>();
    void Update() {
      if (_wireMaterial == null) {
        if (_wireShader == null) {
          return;
        }
        _wireMaterial = new Material(_wireShader);
        _wireMaterial.name = "Runtime Gizmos Wire Material";
        _wireMaterial.hideFlags = HideFlags.HideAndDontSave;
      }

      if (_filledMaterial == null) {
        if (_filledShader == null) {
          return;
        }
        _filledMaterial = new Material(_filledShader);
        _filledMaterial.name = "Runtime Gizmos Filled Material";
        _filledMaterial.hideFlags = HideFlags.HideAndDontSave;
      }

      RGizmos.ClearAllGizmos();

      Scene scene = SceneManager.GetActiveScene();
      scene.GetRootGameObjects(_objList);
      for (int i = 0; i < _objList.Count; i++) {
        GameObject obj = _objList[i];
        obj.GetComponentsInChildren(false, _gizmoList);
        for (int j = 0; j < _gizmoList.Count; j++) {
          Transform componentTransform = (_gizmoList[j] as Component).transform;

          bool isDisabled = false;
          do {
            var toggle = componentTransform.GetComponentInParent<RuntimeGizmoToggle>();
            if (toggle == null) {
              break;
            }

            if (!toggle.enabled) {
              isDisabled = true;
              break;
            }

            componentTransform = componentTransform.parent;
          } while (componentTransform != null);

          if (isDisabled) {
            continue;
          }

          RGizmos.sphereMesh = _sphereMesh;
          RGizmos.cubeMesh = _cubeMesh;
          RGizmos.wireSphereMesh = _wireSphereMesh;
          RGizmos.wireCubeMesh = _wireCubeMesh;

          if (RGizmos.matrix != Matrix4x4.identity) {
            RGizmos.matrix = Matrix4x4.identity;
          }
          _gizmoList[j].OnDrawRuntimeGizmos();
        }
      }
    }

    private void generateMeshes() {
      _cubeMesh = new Mesh();
      _cubeMesh.name = "RuntimeGizmoCube";
      _cubeMesh.hideFlags = HideFlags.HideAndDontSave;

      List<Vector3> verts = new List<Vector3>();
      List<int> indexes = new List<int>();

      addQuad(verts, indexes, Vector3.forward, -Vector3.right, Vector3.up);
      addQuad(verts, indexes, -Vector3.forward, Vector3.right, Vector3.up);
      addQuad(verts, indexes, Vector3.right, -Vector3.up, Vector3.forward);
      addQuad(verts, indexes, -Vector3.right, Vector3.up, Vector3.forward);
      addQuad(verts, indexes, Vector3.up, -Vector3.forward, Vector3.right);
      addQuad(verts, indexes, -Vector3.up, Vector3.forward, Vector3.right);

      _cubeMesh.SetVertices(verts);
      _cubeMesh.SetIndices(indexes.ToArray(), MeshTopology.Quads, 0);
      _cubeMesh.RecalculateNormals();
      _cubeMesh.RecalculateBounds();
      _cubeMesh.UploadMeshData(true);

      _wireCubeMesh = new Mesh();
      _wireCubeMesh.name = "RuntimeWireCubeMesh";
      _wireCubeMesh.hideFlags = HideFlags.HideAndDontSave;

      verts.Clear();
      indexes.Clear();

      verts.Add(0.5f * new Vector3(1, 1, 1));
      verts.Add(0.5f * new Vector3(1, 1, -1));
      verts.Add(0.5f * new Vector3(1, -1, 1));
      verts.Add(0.5f * new Vector3(1, -1, -1));
      verts.Add(0.5f * new Vector3(-1, 1, 1));
      verts.Add(0.5f * new Vector3(-1, 1, -1));
      verts.Add(0.5f * new Vector3(-1, -1, 1));
      verts.Add(0.5f * new Vector3(-1, -1, -1));

      addCorner(indexes, 0, 1, 2, 4);
      addCorner(indexes, 3, 1, 2, 7);
      addCorner(indexes, 5, 1, 4, 7);
      addCorner(indexes, 6, 2, 4, 7);

      _wireCubeMesh.SetVertices(verts);
      _wireCubeMesh.SetIndices(indexes.ToArray(), MeshTopology.Lines, 0);
      _wireCubeMesh.RecalculateBounds();
      _wireCubeMesh.UploadMeshData(true);

      _wireSphereMesh = new Mesh();
      _wireSphereMesh.name = "RuntimeWireSphereMesh";
      _wireSphereMesh.hideFlags = HideFlags.HideAndDontSave;

      verts.Clear();
      indexes.Clear();

      int totalVerts = CIRCLE_RESOLUTION * 3;
      for (int i = 0; i < CIRCLE_RESOLUTION; i++) {
        float angle = Mathf.PI * 2 * i / CIRCLE_RESOLUTION;
        float dx = 0.5f * Mathf.Cos(angle);
        float dy = 0.5f * Mathf.Sin(angle);

        indexes.Add((i * 3 + 0) % totalVerts);
        indexes.Add((i * 3 + 3) % totalVerts);

        indexes.Add((i * 3 + 1) % totalVerts);
        indexes.Add((i * 3 + 4) % totalVerts);

        indexes.Add((i * 3 + 2) % totalVerts);
        indexes.Add((i * 3 + 5) % totalVerts);

        verts.Add(new Vector3(dx, dy, 0));
        verts.Add(new Vector3(0, dx, dy));
        verts.Add(new Vector3(dx, 0, dy));
      }

      _wireSphereMesh.SetVertices(verts);
      _wireSphereMesh.SetIndices(indexes.ToArray(), MeshTopology.Lines, 0);
      _wireSphereMesh.RecalculateBounds();
      _wireSphereMesh.UploadMeshData(true);
    }

    private void addQuad(List<Vector3> verts, List<int> indexes, Vector3 normal, Vector3 axis1, Vector3 axis2) {
      indexes.Add(verts.Count + 0);
      indexes.Add(verts.Count + 1);
      indexes.Add(verts.Count + 2);
      indexes.Add(verts.Count + 3);

      verts.Add(0.5f * (normal + axis1 + axis2));
      verts.Add(0.5f * (normal + axis1 - axis2));
      verts.Add(0.5f * (normal - axis1 - axis2));
      verts.Add(0.5f * (normal - axis1 + axis2));
    }

    private void addCorner(List<int> indexes, int a, int b, int c, int d) {
      indexes.Add(a); indexes.Add(b);
      indexes.Add(a); indexes.Add(c);
      indexes.Add(a); indexes.Add(d);
    }
  }
}
