using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace Leap.Unity.RuntimeGizmos {

  public interface IRuntimeGizmoDrawer {
    void OnDrawRuntimeGizmos();
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

    private List<OperationType> _operations = new List<OperationType>();
    private List<Matrix4x4> _matrices = new List<Matrix4x4>();
    private List<Color> _colors = new List<Color>();
    private List<int> _lineCounts = new List<int>();

    private List<Vector3> _verts = new List<Vector3>();
    private List<Mesh> _meshes = new List<Mesh>();
    private bool _clearOnWrite = false;

    private Material _wireMaterial, _filledMaterial;

    public void MultMatrix(Matrix4x4 matrix) {
      _operations.Add(OperationType.MultMatrix);
      _matrices.Add(matrix);
    }

    public void RelativeTo(Transform transform) {
      MultMatrix(transform.localToWorldMatrix);
    }

    public void PushMatrix() {
      _operations.Add(OperationType.PushMatrix);
    }

    public void PopMatrix() {
      _operations.Add(OperationType.PopMatrix);
    }

    public Color color {
      set {
        _operations.Add(OperationType.Color);
        _colors.Add(value);
      }
    }

    public void DrawMesh(Mesh mesh, Matrix4x4 matrix) {
      _operations.Add(OperationType.Mesh);
      _meshes.Add(mesh);
      _matrices.Add(matrix);
    }

    public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) {
      DrawMesh(mesh, Matrix4x4.TRS(position, rotation, scale));
    }

    public void DrawLine(Vector3 a, Vector3 b) {
      if (_operations[_operations.Count - 1] != OperationType.Lines) {
        _operations.Add(OperationType.Lines);
        _lineCounts.Add(0);
      }

      _lineCounts[_lineCounts.Count - 1]++;
      _verts.Add(a);
      _verts.Add(b);
    }

    public void DrawCube(Vector3 position, Vector3 size) {
      _operations.Add(OperationType.Mesh);
      _meshes.Add(_cubeMesh);
      _matrices.Add(Matrix4x4.TRS(position, Quaternion.identity, size));
    }

    public void DrawWireCube(Vector3 position, Vector3 size) {
      Vector3 p000 = position + new Vector3(size.x, size.y, size.z) * 0.5f;
      Vector3 p001 = position + new Vector3(size.x, size.y, -size.z) * 0.5f;
      Vector3 p010 = position + new Vector3(size.x, -size.y, size.z) * 0.5f;
      Vector3 p011 = position + new Vector3(size.x, -size.y, -size.z) * 0.5f;
      Vector3 p100 = position + new Vector3(-size.x, size.y, size.z) * 0.5f;
      Vector3 p101 = position + new Vector3(-size.x, size.y, -size.z) * 0.5f;
      Vector3 p110 = position + new Vector3(-size.x, -size.y, size.z) * 0.5f;
      Vector3 p111 = position + new Vector3(-size.x, -size.y, -size.z) * 0.5f;

      DrawLine(p000, p001);
      DrawLine(p000, p010);
      DrawLine(p000, p100);

      DrawLine(p011, p010);
      DrawLine(p011, p001);
      DrawLine(p011, p111);

      DrawLine(p110, p111);
      DrawLine(p110, p100);
      DrawLine(p110, p010);

      DrawLine(p101, p100);
      DrawLine(p101, p111);
      DrawLine(p101, p001);
    }

    public void DrawSphere(Vector3 center, float radius) {
      _operations.Add(OperationType.Mesh);
      _meshes.Add(_sphereMesh);
      _matrices.Add(Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * radius));
    }

    public void DrawWireSphere(Vector3 center, float radius) {
      float prevDx = 0, prevDy = 0;

      for (int i = 0; i <= CIRCLE_RESOLUTION; i++) {
        float angle = Mathf.PI * 2 * i / CIRCLE_RESOLUTION;
        float dx = radius * Mathf.Cos(angle);
        float dy = radius * Mathf.Sin(angle);

        if (i != 0) {
          DrawLine(center + new Vector3(dx, dy, 0), center + new Vector3(prevDx, prevDy, 0));
          DrawLine(center + new Vector3(0, dx, dy), center + new Vector3(0, prevDx, prevDy));
          DrawLine(center + new Vector3(dx, 0, dy), center + new Vector3(prevDx, 0, prevDy));
        }

        prevDx = dx;
        prevDy = dy;
      }
    }

    private void onPostRender(Camera camera) {
      bool isSceneCamera = camera.gameObject.hideFlags == HideFlags.HideAndDontSave;
      if (!isSceneCamera && !displayInGameView) {
        return;
      }

      int matrixIndex = 0;
      int colorIndex = 0;
      int lineIndex = 0;

      int vertIndex = 0;
      int meshIndex = 0;

      Color currColor = Color.white;
      Material currMat = null;

      for (int i = 0; i < _operations.Count; i++) {
        OperationType type = _operations[i];
        switch (type) {
          case OperationType.PushMatrix:
            GL.PushMatrix();
            break;
          case OperationType.PopMatrix:
            GL.PopMatrix();
            break;
          case OperationType.MultMatrix:
            GL.MultMatrix(_matrices[matrixIndex++]);
            break;
          case OperationType.Color:
            currColor = _colors[colorIndex++];
            currMat = null;
            break;
          case OperationType.Lines:
            if (currMat != _wireMaterial) {
              currMat = _wireMaterial;
              currMat.color = currColor;
              currMat.SetPass(0);
            }

            GL.Begin(GL.LINES);
            int vertCount = _lineCounts[lineIndex++] * 2;
            for (int j = vertCount; j-- != 0;) {
              GL.Vertex(_verts[vertIndex++]);
            }
            GL.End();
            break;
          case OperationType.Mesh:
            if (currMat != _filledMaterial) {
              currMat = _filledMaterial;
              currMat.color = currColor;
              currMat.SetPass(0);
            }

            Graphics.DrawMeshNow(_meshes[meshIndex++], _matrices[matrixIndex++]);
            break;
          default:
            throw new InvalidOperationException("Unexpected operation type " + type);
        }
      }
    }

    void Awake() {
      _wireMaterial = new Material(_wireShader);
      _filledMaterial = new Material(_filledShader);
    }

    void OnEnable() {
      Camera.onPostRender -= onPostRender;
      Camera.onPostRender += onPostRender;
    }

    void OnDisable() {
      Camera.onPostRender -= onPostRender;
    }

    private List<GameObject> _objList = new List<GameObject>();
    private List<IRuntimeGizmoDrawer> _gizmoList = new List<IRuntimeGizmoDrawer>();
    void Update() {
      clear();

      Scene scene = SceneManager.GetActiveScene();
      scene.GetRootGameObjects(_objList);
      for (int i = 0; i < _objList.Count; i++) {
        GameObject obj = _objList[i];
        obj.GetComponentsInChildren(false, _gizmoList);
        for (int j = 0; j < _gizmoList.Count; j++) {
          _gizmoList[j].OnDrawRuntimeGizmos();
        }
      }
    }

    private void clear() {
      _operations.Clear();
      _matrices.Clear();
      _colors.Clear();
      _lineCounts.Clear();
      _verts.Clear();
      _meshes.Clear();
    }

    private enum OperationType {
      PushMatrix,
      PopMatrix,
      MultMatrix,
      Color,
      Lines,
      Mesh
    }
  }
}
