using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace Leap.Unity.RuntimeGizmos {

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

    public static Mesh cubeMesh, sphereMesh;

    public static void RelativeTo(Transform transform) {
      matrix = transform.localToWorldMatrix;
    }

    public static void PushMatrix() {
      _matrixStack.Push(_currMatrix);
    }

    public static void PopMatrix() {
      matrix = _matrixStack.Pop();
    }

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

    public static void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
      setWireMode(false);
      drawMeshInternal(mesh, matrix);
    }

    public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    public static void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {
      setWireMode(true);
      drawMeshInternal(mesh, matrix);
    }

    public static void DrawWireMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawWireMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    public static void DrawLine(Vector3 a, Vector3 b) {
      _operations.Add(OperationType.DrawLine);
      _lines.Add(new Line(a, b));
    }

    public static void DrawCube(Vector3 position, Vector3 size) {
      DrawMesh(cubeMesh, position, Quaternion.identity, size);
    }

    public static void DrawWireCube(Vector3 position, Vector3 size) {
      DrawWireMesh(cubeMesh, position, Quaternion.identity, size);
    }

    public static void DrawSphere(Vector3 center, float radius) {
      DrawMesh(sphereMesh, center, Quaternion.identity, Vector3.one * radius * 2);
    }

    public static void DrawWireSphere(Vector3 center, float radius) {
      DrawWireMesh(sphereMesh, center, Quaternion.identity, Vector3.one * radius * 2);
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

      GL.wireframe = false;
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
    public const int CIRCLE_RESOLUTION = 16;

    [SerializeField]
    private bool displayInGameView = true;

    [SerializeField]
    private Mesh _cubeMesh;

    [SerializeField]
    private Mesh _sphereMesh;

    [SerializeField]
    private Shader _wireShader;

    [SerializeField]
    private Shader _filledShader;

    private Material _wireMaterial, _filledMaterial;

    private void onPostRender(Camera camera) {
      bool isSceneCamera = camera.gameObject.hideFlags == HideFlags.HideAndDontSave;
      if (!isSceneCamera && !displayInGameView) {
        return;
      }

      RGizmos.DrawAllGizmosToScreen(_wireMaterial, _filledMaterial);
    }

    void OnEnable() {
      _wireMaterial = new Material(_wireShader);
      _filledMaterial = new Material(_filledShader);

      Camera.onPostRender -= onPostRender;
      Camera.onPostRender += onPostRender;
    }

    void OnDisable() {
      Camera.onPostRender -= onPostRender;
    }

    private List<GameObject> _objList = new List<GameObject>();
    private List<IRuntimeGizmoDrawer> _gizmoList = new List<IRuntimeGizmoDrawer>();
    void Update() {
      RGizmos.ClearAllGizmos();
      RGizmos.sphereMesh = _sphereMesh;
      RGizmos.cubeMesh = _cubeMesh;

      Scene scene = SceneManager.GetActiveScene();
      scene.GetRootGameObjects(_objList);
      for (int i = 0; i < _objList.Count; i++) {
        GameObject obj = _objList[i];
        obj.GetComponentsInChildren(false, _gizmoList);
        for (int j = 0; j < _gizmoList.Count; j++) {
          if (RGizmos.matrix != Matrix4x4.identity) {
            RGizmos.matrix = Matrix4x4.identity;
          }
          _gizmoList[j].OnDrawRuntimeGizmos();
        }
      }
    }
  }
}
