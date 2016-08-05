using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity.RuntimeGizmos {

  /// <summary>
  /// Have your MonoBehaviour implement this interface to be able to draw runtime gizmos.
  /// Remember that you must use the RGizmos class, not Unity's Gizmos class, for runtime
  /// gizmos to work!  You must also have a RuntimeGizmoDrawer component in the scene.
  /// </summary>
  public interface IRuntimeGizmoDrawer {
    void OnDrawRuntimeGizmos(RGizmos drawer);
  }

  [ExecuteInEditMode]
  public class RuntimeGizmoDrawer : MonoBehaviour {
    public const int CIRCLE_RESOLUTION = 32;

    [Tooltip("Should the gizmos be visible in the game view.")]
    [SerializeField]
    protected bool _displayInGameView = true;

    [Tooltip("Should the gizmos be visible in a build.")]
    [SerializeField]
    protected bool _enabledForBuild = true;

    [Tooltip("The mesh to use for the filled sphere gizmo.")]
    [SerializeField]
    protected Mesh _sphereMesh;

    [Tooltip("The shader to use for rendering wire gizmos.")]
    [SerializeField]
    protected Shader _wireShader;

    [Tooltip("The shader to use for rendering filled gizmos.")]
    [SerializeField]
    protected Shader _filledShader;

    protected Material _wireMaterial, _filledMaterial;
    protected Mesh _cubeMesh, _wireCubeMesh, _wireSphereMesh;

    protected static RGizmos _drawer;

    public static RGizmos GetGizmoDrawer() {
      _drawer.ResetMatrixAndColorState();
      return _drawer;
    }

    protected void onPostRender(Camera camera) {
#if UNITY_EDITOR
      if (camera.gameObject.name == "PreRenderCamera") {
        return;
      }

      bool isSceneCamera = camera.gameObject.hideFlags == HideFlags.HideAndDontSave;
      if (!isSceneCamera && !_displayInGameView) {
        return;
      }
#endif

      _drawer.DrawAllGizmosToScreen(_wireMaterial, _filledMaterial);
    }

    protected virtual void OnValidate() {
      if (_wireMaterial != null) {
        _wireMaterial.shader = _wireShader;
      }
      if (_filledMaterial != null) {
        _filledMaterial.shader = _filledShader;
      }
    }

    protected virtual void OnEnable() {
#if !UNITY_EDITOR
      if (_enabledForBuild) {
        enabled = false;
      }
#endif

      _drawer = new RGizmos();

      generateMeshes();
      assignMeshes();

      Camera.onPostRender -= onPostRender;
      Camera.onPostRender += onPostRender;

      if (Application.isPlaying) {
        StartCoroutine(clearGizmoCoroutine());
      }
    }

    protected virtual void OnDisable() {
      Camera.onPostRender -= onPostRender;
    }

    private List<GameObject> _objList = new List<GameObject>();
    private List<IRuntimeGizmoDrawer> _gizmoList = new List<IRuntimeGizmoDrawer>();
    protected virtual void Update() {
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

#if UNITY_EDITOR
      //If the application is playing, gizmos are cleared at the end of frame instead
      if (!Application.isPlaying) {
        _drawer.ClearAllGizmos();
      }
#endif

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

          _drawer.ResetMatrixAndColorState();
          assignMeshes();
          
          _gizmoList[j].OnDrawRuntimeGizmos(_drawer);
        }
      }
    }

    private IEnumerator clearGizmoCoroutine() {
      WaitForEndOfFrame endOfFrameWaiter = new WaitForEndOfFrame();
      while (true) {
        yield return endOfFrameWaiter;
        _drawer.ClearAllGizmos();
      }
    }

    private void assignMeshes() {
      _drawer.sphereMesh = _sphereMesh;
      _drawer.cubeMesh = _cubeMesh;
      _drawer.wireSphereMesh = _wireSphereMesh;
      _drawer.wireCubeMesh = _wireCubeMesh;
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

  public class RGizmos {
    private List<OperationType> _operations = new List<OperationType>();
    private List<Matrix4x4> _matrices = new List<Matrix4x4>();
    private List<Color> _colors = new List<Color>();
    private List<Line> _lines = new List<Line>();
    private List<Mesh> _meshes = new List<Mesh>();

    private Color _currColor = Color.white;
    private Matrix4x4 _currMatrix = Matrix4x4.identity;
    private Stack<Matrix4x4> _matrixStack = new Stack<Matrix4x4>();

    private bool _isInWireMode = false;

    public Mesh cubeMesh, wireCubeMesh, sphereMesh, wireSphereMesh;

    /// <summary>
    /// Causes all remaining gizmos drawing to be done in the local coordinate space of the given transform.
    /// </summary>
    public void RelativeTo(Transform transform) {
      matrix = transform.localToWorldMatrix;
    }

    /// <summary>
    /// Saves the current gizmo matrix to the gizmo matrix stack.
    /// </summary>
    public void PushMatrix() {
      _matrixStack.Push(_currMatrix);
    }

    /// <summary>
    /// Restores the current gizmo matrix from the gizmo matrix stack.
    /// </summary>
    public void PopMatrix() {
      matrix = _matrixStack.Pop();
    }

    /// <summary>
    /// Resets the matrix to the identity matrix and the color to white.
    /// </summary>
    public void ResetMatrixAndColorState() {
      if (_currMatrix != Matrix4x4.identity) {
        matrix = Matrix4x4.identity;
      }
      if (_currColor != Color.white) {
        color = Color.white;
      }
    }

    /// <summary>
    /// Sets or gets the color for the gizmos that will be drawn next.
    /// </summary>
    public Color color {
      get {
        return _currColor;
      }
      set {
        if (_currColor == value) {
          return;
        }
        _currColor = value;
        _operations.Add(OperationType.SetColor);
        _colors.Add(_currColor);
      }
    }


    /// <summary>
    /// Sets or gets the matrix used to transform all gizmos.
    /// </summary>
    public Matrix4x4 matrix {
      get {
        return _currMatrix;
      }
      set {
        if (_currMatrix == value) {
          return;
        }
        _currMatrix = value;
        _operations.Add(OperationType.SetMatrix);
        _matrices.Add(_currMatrix);
      }
    }

    /// <summary>
    /// Draw a filled gizmo mesh using the given matrix transform.
    /// </summary>
    public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
      setWireMode(false);
      drawMeshInternal(mesh, matrix);
    }

    /// <summary>
    /// Draws a filled gizmo mesh at the given transform location.
    /// </summary>
    public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    /// <summary>
    /// Draws a wire gizmo mesh using the given matrix transform.
    /// </summary>
    public void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
      setWireMode(true);
      drawMeshInternal(mesh, matrix);
    }

    /// <summary>
    /// Draws a wire gizmo mesh at the given transform location.
    /// </summary>
    public void DrawWireMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawWireMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    /// <summary>
    /// Draws a gizmo line that connects the two positions.
    /// </summary>
    public void DrawLine(Vector3 a, Vector3 b) {
      _operations.Add(OperationType.DrawLine);
      _lines.Add(new Line(a, b));
    }

    /// <summary>
    /// Draws a filled gizmo cube at the given position with the given size.
    /// </summary>
    public void DrawCube(Vector3 position, Vector3 size) {
      DrawMesh(cubeMesh, position, Quaternion.identity, size);
    }

    /// <summary>
    /// Draws a wire gizmo cube at the given position with the given size.
    /// </summary>
    public void DrawWireCube(Vector3 position, Vector3 size) {
      DrawWireMesh(wireCubeMesh, position, Quaternion.identity, size);
    }

    /// <summary>
    /// Draws a filled gizmo sphere at the given position with the given radius.
    /// </summary>
    public void DrawSphere(Vector3 center, float radius) {
      DrawMesh(sphereMesh, center, Quaternion.identity, Vector3.one * radius * 2);
    }

    /// <summary>
    /// Draws a wire gizmo sphere at the given position with the given radius.
    /// </summary>
    public void DrawWireSphere(Vector3 center, float radius) {
      DrawWireMesh(wireSphereMesh, center, Quaternion.identity, Vector3.one * radius * 2);
    }

    private List<Collider> _colliderList = new List<Collider>();
    public void DrawColliders(GameObject gameObject, bool useWireframe = true, bool traverseHierarchy = true) {
      PushMatrix();

      if (traverseHierarchy) {
        gameObject.GetComponentsInChildren(_colliderList);
      } else {
        gameObject.GetComponents(_colliderList);
      }

      for (int i = 0; i < _colliderList.Count; i++) {
        Collider collider = _colliderList[i];
        RelativeTo(collider.transform);

        if (collider is BoxCollider) {
          BoxCollider box = collider as BoxCollider;
          if (useWireframe) {
            DrawWireCube(box.center, box.size);
          } else {
            DrawCube(box.center, box.size);
          }
        } else if (collider is SphereCollider) {
          SphereCollider sphere = collider as SphereCollider;
          if (useWireframe) {
            DrawWireSphere(sphere.center, sphere.radius);
          } else {
            DrawSphere(sphere.center, sphere.radius);
          }
        } else if (collider is CapsuleCollider) {
          CapsuleCollider capsule = collider as CapsuleCollider;
          Vector3 size = Vector3.zero;
          size += Vector3.one * capsule.radius * 2;
          size += new Vector3(capsule.direction == 0 ? 1 : 0,
                              capsule.direction == 1 ? 1 : 0,
                              capsule.direction == 2 ? 1 : 0) * (capsule.height - capsule.radius * 2);
          if (useWireframe) {
            DrawWireCube(capsule.center, size);
          } else {
            DrawCube(capsule.center, size);
          }
        } else if (collider is MeshCollider) {
          MeshCollider mesh = collider as MeshCollider;
          if (mesh.sharedMesh != null) {
            DrawWireMesh(mesh.sharedMesh, Matrix4x4.identity);
          }
        }
      }

      PopMatrix();
    }

    public void ClearAllGizmos() {
      _operations.Clear();
      _matrices.Clear();
      _colors.Clear();
      _lines.Clear();
      _meshes.Clear();
      _isInWireMode = false;
      _currMatrix = Matrix4x4.identity;
      _currColor = Color.white;
    }

    private Material _currMat;
    public void DrawAllGizmosToScreen(Material wireMaterial, Material filledMaterial) {
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

    private void setMaterial(Material mat) {
      if (_currMat != mat) {
        _currMat = mat;
        _currMat.color = _currColor;
        _currMat.SetPass(0);
      }
    }

    private void drawMeshInternal(Mesh mesh, Matrix4x4 matrix) {
      _operations.Add(OperationType.DrawMesh);
      _meshes.Add(mesh);
      _matrices.Add(matrix);
    }

    private void setWireMode(bool wireMode) {
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
}
