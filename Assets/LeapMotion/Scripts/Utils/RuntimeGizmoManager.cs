using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace Leap.Unity.RuntimeGizmos {

  /// <summary>
  /// Have your MonoBehaviour implement this interface to be able to draw runtime gizmos.
  /// You must also have a RuntimeGizmoManager component in the scene to recieve callbacks.
  /// </summary>
  public interface IRuntimeGizmoComponent {
    void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer);
  }

  [ExecuteInEditMode]
  public class RuntimeGizmoManager : MonoBehaviour {
    public const string DEFAULT_SHADER_NAME = "Hidden/Runtime Gizmos";
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

    [Tooltip("The shader to use for rendering gizmos.")]
    [SerializeField]
    protected Shader _gizmoShader;

    protected Mesh _cubeMesh, _wireCubeMesh, _wireSphereMesh;

    protected static RuntimeGizmoDrawer _backDrawer = null;
    protected static RuntimeGizmoDrawer _frontDrawer = null;
    private bool _readyForSwap = false;

    /// <summary>
    /// Subscribe to this event if you want to draw gizmos after rendering is complete.  Doing gizmo
    /// rendering inside of the normal Camera.onPoseRender event will cause rendering artifacts.
    /// </summary>
    public static event Action<RuntimeGizmoDrawer> OnPostRenderGizmos;

    /// <summary>
    /// Tries to get a gizmo drawer.  Will fail if there is no Gizmo manager in the 
    /// scene, or if it is disabled.
    /// 
    /// The gizmo matrix will be set to the identity matrix.
    /// The gizmo color will be set to white.
    /// </summary>
    public static bool TryGetGizmoDrawer(out RuntimeGizmoDrawer drawer) {
      drawer = _backDrawer;
      if (drawer != null) {
        drawer.ResetMatrixAndColorState();
        return true;
      } else {
        return false;
      }
    }

    /// <summary>
    /// Tries to get a gizmo drawer for a given gameObject.  Will fail if there is no
    /// gizmo manager in the scene, or if it is disabled.  Will also fail if there is
    /// a disable RuntimeGizmoToggle as a parent of the gameObject.
    /// 
    /// The gizmo matrix will be set to the identity matrix.
    /// The gizmo color will be set to white.
    /// </summary>
    public static bool TryGetGizmoDrawer(GameObject attatchedGameObject, out RuntimeGizmoDrawer drawer) {
      drawer = _backDrawer;
      if (drawer != null && !areGizmosDisabled(attatchedGameObject.transform)) {
        drawer.ResetMatrixAndColorState();
        return true;
      } else {
        return false;
      }
    }

    protected virtual void OnValidate() {
      if (_gizmoShader == null) {
        _gizmoShader = Shader.Find(DEFAULT_SHADER_NAME);
      }

      Material tempMat = new Material(_gizmoShader);
      tempMat.hideFlags = HideFlags.HideAndDontSave;
      if (tempMat.passCount != 4) {
        Debug.LogError("Shader " + _gizmoShader + " does not have 4 passes and cannot be used as a gizmo shader.");
        _gizmoShader = Shader.Find(DEFAULT_SHADER_NAME);
      }

      if (_sphereMesh == null) {
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tempSphere.hideFlags = HideFlags.HideAndDontSave;
        _sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
        tempSphere.GetComponent<MeshFilter>().sharedMesh = null;
      }

      if (_frontDrawer != null && _backDrawer != null) {
        assignDrawerParams();
      }
    }

    protected virtual void Reset() {
      _gizmoShader = Shader.Find(DEFAULT_SHADER_NAME);
    }

    protected virtual void OnEnable() {
#if !UNITY_EDITOR
      if (!_enabledForBuild) {
        enabled = false;
        return;
      }
#endif

      _frontDrawer = new RuntimeGizmoDrawer();
      _backDrawer = new RuntimeGizmoDrawer();

      _frontDrawer.BeginGuard();

      if (_gizmoShader == null) {
        _gizmoShader = Shader.Find(DEFAULT_SHADER_NAME);
      }

      generateMeshes();
      assignDrawerParams();

      //Unsubscribe to prevent double-subscription
      Camera.onPostRender -= onPostRender;
      Camera.onPostRender += onPostRender;
    }

    protected virtual void OnDisable() {
      _frontDrawer = null;
      _backDrawer = null;

      Camera.onPostRender -= onPostRender;
    }

    private List<GameObject> _objList = new List<GameObject>();
    private List<IRuntimeGizmoComponent> _gizmoList = new List<IRuntimeGizmoComponent>();
    protected virtual void Update() {
      Scene scene = SceneManager.GetActiveScene();
      scene.GetRootGameObjects(_objList);
      for (int i = 0; i < _objList.Count; i++) {
        GameObject obj = _objList[i];
        obj.GetComponentsInChildren(false, _gizmoList);
        for (int j = 0; j < _gizmoList.Count; j++) {
          if (areGizmosDisabled((_gizmoList[j] as Component).transform)) {
            continue;
          }

          _backDrawer.ResetMatrixAndColorState();

          try {
            _gizmoList[j].OnDrawRuntimeGizmos(_backDrawer);
          } catch (Exception e) {
            Debug.LogException(e);
          }
        }
      }

      _readyForSwap = true;
    }

    protected void onPostRender(Camera camera) {
#if UNITY_EDITOR
      //Hard-coded name of the camera used to generate the pre-render view
      if (camera.gameObject.name == "PreRenderCamera") {
        return;
      }

      bool isSceneCamera = camera.gameObject.hideFlags == HideFlags.HideAndDontSave;
      if (!isSceneCamera && !_displayInGameView) {
        return;
      }
#endif

      if (_readyForSwap) {
        if (OnPostRenderGizmos != null) {
          _backDrawer.ResetMatrixAndColorState();
          OnPostRenderGizmos(_backDrawer);
        }

        RuntimeGizmoDrawer tempDrawer = _backDrawer;
        _backDrawer = _frontDrawer;
        _frontDrawer = tempDrawer;

        //Guard the front drawer for rendering
        _frontDrawer.BeginGuard();

        //Unguard the back drawer to allow gizmos to be drawn to it
        _backDrawer.EndGuard();

        _readyForSwap = false;
        _backDrawer.ClearAllGizmos();
      }

      _frontDrawer.DrawAllGizmosToScreen();
    }

    protected static bool areGizmosDisabled(Transform transform) {
      bool isDisabled = false;
      do {
        var toggle = transform.GetComponentInParent<RuntimeGizmoToggle>();
        if (toggle == null) {
          break;
        }

        if (!toggle.enabled) {
          isDisabled = true;
          break;
        }

        transform = transform.parent;
      } while (transform != null);

      return isDisabled;
    }

    private void assignDrawerParams() {
      if (_gizmoShader != null) {
        _frontDrawer.gizmoShader = _gizmoShader;
        _backDrawer.gizmoShader = _gizmoShader;
      }

      _frontDrawer.sphereMesh = _sphereMesh;
      _frontDrawer.cubeMesh = _cubeMesh;
      _frontDrawer.wireSphereMesh = _wireSphereMesh;
      _frontDrawer.wireCubeMesh = _wireCubeMesh;

      _backDrawer.sphereMesh = _sphereMesh;
      _backDrawer.cubeMesh = _cubeMesh;
      _backDrawer.wireSphereMesh = _wireSphereMesh;
      _backDrawer.wireCubeMesh = _wireCubeMesh;
    }

    private void generateMeshes() {
      _cubeMesh = new Mesh();
      _cubeMesh.name = "RuntimeGizmoCube";
      _cubeMesh.hideFlags = HideFlags.HideAndDontSave;

      List<Vector3> verts = new List<Vector3>();
      List<int> indexes = new List<int>();

      Vector3[] faces = new Vector3[] { Vector3.forward, Vector3.right, Vector3.up };
      for (int i = 0; i < 3; i++) {
        addQuad(verts, indexes, faces[(i + 0) % 3], -faces[(i + 1) % 3], faces[(i + 2) % 3]);
        addQuad(verts, indexes, -faces[(i + 0) % 3], faces[(i + 1) % 3], faces[(i + 2) % 3]);
      }

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

      for (int dx = 1; dx >= -1; dx -= 2) {
        for (int dy = 1; dy >= -1; dy -= 2) {
          for (int dz = 1; dz >= -1; dz -= 2) {
            verts.Add(0.5f * new Vector3(dx, dy, dz));
          }
        }
      }

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

        for (int j = 0; j < 3; j++) {
          indexes.Add((i * 3 + j + 0) % totalVerts);
          indexes.Add((i * 3 + j + 3) % totalVerts);
        }

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

  public class RuntimeGizmoDrawer {
    public const int UNLIT_SOLID_PASS = 0;
    public const int UNLIT_TRANSPARENT_PASS = 1;
    public const int SHADED_SOLID_PASS = 2;
    public const int SHADED_TRANSPARENT_PASS = 3;

    private List<OperationType> _operations = new List<OperationType>();
    private List<Matrix4x4> _matrices = new List<Matrix4x4>();
    private List<Color> _colors = new List<Color>();
    private List<Line> _lines = new List<Line>();
    private List<Mesh> _meshes = new List<Mesh>();

    private Color _currColor = Color.white;
    private Matrix4x4 _currMatrix = Matrix4x4.identity;
    private Stack<Matrix4x4> _matrixStack = new Stack<Matrix4x4>();

    private bool _isInWireMode = false;
    private Material _gizmoMaterial;
    private int _operationCountOnGuard = -1;

    public Shader gizmoShader {
      get {
        if (_gizmoMaterial == null) {
          return null;
        } else {
          return _gizmoMaterial.shader;
        }
      }
      set {
        if (_gizmoMaterial == null) {
          _gizmoMaterial = new Material(value);
          _gizmoMaterial.name = "Runtime Gizmo Material";
          _gizmoMaterial.hideFlags = HideFlags.HideAndDontSave;
        } else {
          _gizmoMaterial.shader = value;
        }
      }
    }

    public Mesh cubeMesh, wireCubeMesh, sphereMesh, wireSphereMesh;

    /// <summary>
    /// Begins a draw-guard.  If any gizmos are drawn to this drawer an exception will be thrown at the end of the guard.
    /// </summary>
    public void BeginGuard() {
      _operationCountOnGuard = _operations.Count;
    }

    /// <summary>
    /// Ends a draw-guard.  If any gizmos were drawn to this drawer during the guard, an exception will be thrown.
    /// </summary>
    public void EndGuard() {
      bool wereGizmosDrawn = _operations.Count > _operationCountOnGuard;
      _operationCountOnGuard = -1;

      if (wereGizmosDrawn) {
        Debug.LogError("New gizmos were drawn to the front buffer!  Make sure to never keep a reference to a Drawer, always get a new one every time you want to start drawing.");
      }
    }

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
      matrix = Matrix4x4.identity;
      color = Color.white;
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

    /// <summary>
    /// Draws a wire gizmo circle at the given position, with the given normal and radius.
    /// </summary>
    public void DrawWireCirlce(Vector3 center, Vector3 direction, float radius) {
      PushMatrix();
      matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(direction), new Vector3(1, 1, 0)) * matrix;
      DrawWireSphere(Vector3.zero, radius);
      PopMatrix();
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
            if (useWireframe) {
              DrawWireMesh(mesh.sharedMesh, Matrix4x4.identity);
            } else {
              DrawMesh(mesh.sharedMesh, Matrix4x4.identity);
            }
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

    public void DrawAllGizmosToScreen() {
      try {
        int matrixIndex = 0;
        int colorIndex = 0;
        int lineIndex = 0;
        int meshIndex = 0;

        int currPass = -1;
        _currMatrix = Matrix4x4.identity;
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
              currPass = -1; //force pass to be set the next time we need to draw
              break;
            case OperationType.ToggleWireframe:
              GL.wireframe = !GL.wireframe;
              break;
            case OperationType.DrawLine:
              setPass(ref currPass, isUnlit: true);

              GL.Begin(GL.LINES);
              Line line = _lines[lineIndex++];
              GL.Vertex(_currMatrix.MultiplyPoint(line.a));
              GL.Vertex(_currMatrix.MultiplyPoint(line.b));
              GL.End();
              break;
            case OperationType.DrawMesh:
              if (GL.wireframe) {
                setPass(ref currPass, isUnlit: true);
              } else {
                setPass(ref currPass, isUnlit: false);
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

    private void setPass(ref int currPass, bool isUnlit) {
      int newPass;
      if (isUnlit) {
        if (_currColor.a < 1) {
          newPass = UNLIT_TRANSPARENT_PASS;
        } else {
          newPass = UNLIT_SOLID_PASS;
        }
      } else {
        if (_currColor.a < 1) {
          newPass = SHADED_TRANSPARENT_PASS;
        } else {
          newPass = SHADED_SOLID_PASS;
        }
      }

      if (currPass != newPass) {
        currPass = newPass;
        _gizmoMaterial.color = _currColor;
        _gizmoMaterial.SetPass(currPass);
      }
    }

    private void drawMeshInternal(Mesh mesh, Matrix4x4 matrix) {
      if (mesh == null) {
        throw new InvalidOperationException("Mesh cannot be null!");
      }
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
