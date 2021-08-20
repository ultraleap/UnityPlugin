/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Leap.Unity.Infix;

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
    /// rendering inside of the normal Camera.onPostRender event will cause rendering artifacts.
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
      if ((camera.cullingMask & gameObject.layer) == 0) { return; }

#if UNITY_EDITOR
      //Always draw scene view
      //Never draw preview or reflection
      switch (camera.cameraType) {
        case CameraType.Preview:
#if UNITY_2017_1_OR_NEWER
        case CameraType.Reflection:
#endif
          return;
        case CameraType.Game:
        case CameraType.VR:
          if (!_displayInGameView) {
            return;
          }
          break;
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
    private List<WireSphere> _wireSpheres = new List<WireSphere>();
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
      //Throw an error here so we can give a more specific error than the more
      //general one which will be thrown later for a null mesh.
      if (sphereMesh == null) {
        throw new InvalidOperationException("Cannot draw a sphere because the Runtime Gizmo Manager does not have a sphere mesh assigned!");
      }

      DrawMesh(sphereMesh, center, Quaternion.identity, Vector3.one * radius * 2);
    }

    public void DrawWireSphere(Pose pose, float radius, int numSegments = 32) {
      _operations.Add(OperationType.DrawWireSphere);
      _wireSpheres.Add(new WireSphere() {
        pose = pose,
        radius = radius,
        numSegments = numSegments
      });
    }

    /// <summary>
    /// Draws a wire gizmo sphere at the given position with the given radius.
    /// </summary>
    public void DrawWireSphere(Vector3 center, float radius, int numSegments = 32) {
      DrawWireSphere(new Pose(center, Quaternion.identity), radius, numSegments);
    }

    /// <summary>
    /// Draws a wire ellipsoid gizmo with two specified foci and a specified minor axis
    /// length.
    /// </summary>
    public void DrawEllipsoid(Vector3 foci1, Vector3 foci2, float minorAxis) {
      PushMatrix();
      Vector3 ellipseCenter = (foci1 + foci2) / 2f;
      Quaternion ellipseRotation = Quaternion.LookRotation(foci1 - foci2);
      var majorAxis = Mathf.Sqrt(Mathf.Pow(Vector3.Distance(foci1, foci2) / 2f, 2f)
                                 + Mathf.Pow(minorAxis / 2f, 2f)) * 2f;
      Vector3 ellipseScale = new Vector3(minorAxis, minorAxis, majorAxis);

      matrix = Matrix4x4.TRS(ellipseCenter, ellipseRotation, ellipseScale);

      DrawWireSphere(Vector3.zero, 0.5f);
      PopMatrix();
    }

    /// <summary>
    /// Draws a wire gizmo capsule at the given position, with the given start and end
    /// points and radius.
    /// </summary>
    public void DrawWireCapsule(Vector3 start, Vector3 end, float radius) {
      Vector3 up = (end - start).normalized * radius;
      Vector3 forward = Vector3.Slerp(up, -up, 0.5F);
      Vector3 right = Vector3.Cross(up, forward).normalized * radius;

      float height = (start - end).magnitude;

      // Radial circles
      DrawLineWireCircle(start, up, radius, 8);
      DrawLineWireCircle(end, -up, radius, 8);

      // Sides
      DrawLine(start + right, end + right);
      DrawLine(start - right, end - right);
      DrawLine(start + forward, end + forward);
      DrawLine(start - forward, end - forward);

      // Endcaps
      DrawWireArc(start, right, forward, radius, 0.5F, 8);
      DrawWireArc(start, forward, -right, radius, 0.5F, 8);
      DrawWireArc(end, right, -forward, radius, 0.5F, 8);
      DrawWireArc(end, forward, right, radius, 0.5F, 8);
    }

    private void DrawLineWireCircle(Vector3 center, Vector3 normal, float radius, int numCircleSegments = 16) {
      DrawWireArc(center, normal, Vector3.Slerp(normal, -normal, 0.5F), radius, 1.0F, numCircleSegments);
    }

    public void DrawWireArc(Vector3 center, Vector3 normal, Vector3 radialStartDirection,
                            float radius, float fractionOfCircleToDraw, int numCircleSegments = 16) {
      normal = normal.normalized;
      Vector3 radiusVector = radialStartDirection.normalized * radius;
      Vector3 nextVector;
      int numSegmentsToDraw = (int)(numCircleSegments * fractionOfCircleToDraw);
      Quaternion rotator = Quaternion.AngleAxis(360f / numCircleSegments, normal);
      for (int i = 0; i < numSegmentsToDraw; i++) {
        nextVector = rotator * radiusVector;
        DrawLine(center + radiusVector, center + nextVector);
        radiusVector = nextVector;
      }
    }

    private List<Collider> _colliderList = new List<Collider>();
    public void DrawColliders(GameObject gameObject, bool useWireframe = true,
                                                     bool traverseHierarchy = true,
                                                     bool drawTriggers = false) {
      PushMatrix();

      if (traverseHierarchy) {
        gameObject.GetComponentsInChildren(_colliderList);
      } else {
        gameObject.GetComponents(_colliderList);
      }

      for (int i = 0; i < _colliderList.Count; i++) {
        Collider collider = _colliderList[i];
        RelativeTo(collider.transform);

        if (collider.isTrigger && !drawTriggers) { continue; }

        DrawCollider(collider, skipMatrixSetup: true);
      }

      PopMatrix();
    }

    public void DrawCollider(Collider collider, bool useWireframe = true,
                                                bool skipMatrixSetup = false) {
      if (!skipMatrixSetup) {
        PushMatrix();
        RelativeTo(collider.transform);
      }

      if (collider is BoxCollider) {
        BoxCollider box = collider as BoxCollider;
        if (useWireframe) {
          DrawWireCube(box.center, box.size);
        }
        else {
          DrawCube(box.center, box.size);
        }
      }
      else if (collider is SphereCollider) {
        SphereCollider sphere = collider as SphereCollider;
        if (useWireframe) {
          DrawWireSphere(sphere.center, sphere.radius);
        }
        else {
          DrawSphere(sphere.center, sphere.radius);
        }
      }
      else if (collider is CapsuleCollider) {
        CapsuleCollider capsule = collider as CapsuleCollider;
        if (useWireframe) {
          Vector3 capsuleDir;
          switch (capsule.direction) {
            case 0: capsuleDir = Vector3.right; break;
            case 1: capsuleDir = Vector3.up; break;
            case 2: default: capsuleDir = Vector3.forward; break;
          }
          DrawWireCapsule(capsule.center + capsuleDir * (capsule.height / 2F - capsule.radius),
                          capsule.center - capsuleDir * (capsule.height / 2F - capsule.radius), capsule.radius);
        }
        else {
          Vector3 size = Vector3.zero;
          size += Vector3.one * capsule.radius * 2;
          size += new Vector3(capsule.direction == 0 ? 1 : 0,
                              capsule.direction == 1 ? 1 : 0,
                              capsule.direction == 2 ? 1 : 0) * (capsule.height - capsule.radius * 2);
          DrawCube(capsule.center, size);
        }
      }
      else if (collider is MeshCollider) {
        MeshCollider mesh = collider as MeshCollider;
        if (mesh.sharedMesh != null) {
          if (useWireframe) {
            DrawWireMesh(mesh.sharedMesh, Matrix4x4.identity);
          }
          else {
            DrawMesh(mesh.sharedMesh, Matrix4x4.identity);
          }
        }
      }

      if (!skipMatrixSetup) {
        PopMatrix();
      }
    }

    /// <summary>
    /// Draws a simple XYZ-cross position gizmo at the target position, whose size is
    /// scaled relative to the main camera's distance to the target position (for reliable
    /// visibility).
    /// 
    /// Or, if you provide an override scale, you can enforce a radius size for the gizmo.
    /// 
    /// You can also provide a color argument and lerp coefficient towards that color from
    /// the axes' default colors (red, green, blue). Colors are lerped in HSV space.
    /// </summary>
    public void DrawPosition(Vector3 pos, Color lerpColor, float lerpCoeff, float? overrideScale = null) {
      float targetScale;
      if (overrideScale.HasValue) {
        targetScale = overrideScale.Value;
      }
      else {
        targetScale = 0.06f; // 6 cm at 1m away.

        var curCam = Camera.current;
        var posWorldSpace = matrix * pos;
        if (curCam != null) {
          float camDistance = Vector3.Distance(posWorldSpace, curCam.transform.position);

          targetScale *= camDistance;
        }
      }

      float extent = (targetScale / 2f);

      float negativeAlpha = 0.6f;

      color = Color.red;
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos, pos + Vector3.right * extent);
      color = Color.black.WithAlpha(negativeAlpha);
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos, pos - Vector3.right * extent);

      color = Color.green;
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos, pos + Vector3.up * extent);
      color = Color.black.WithAlpha(negativeAlpha);
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos, pos - Vector3.up * extent);

      color = Color.blue;
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos, pos + Vector3.forward * extent);
      color = Color.black.WithAlpha(negativeAlpha);
      if (lerpCoeff != 0f) { color = color.LerpHSV(lerpColor, lerpCoeff); }
      DrawLine(pos, pos - Vector3.forward * extent);
    }

    /// <summary>
    /// Draws a simple XYZ-cross position gizmo at the target position, whose size is
    /// scaled relative to the main camera's distance to the target position (for reliable
    /// visibility).
    /// </summary>
    public void DrawPosition(Vector3 pos) {
      DrawPosition(pos, Color.white, 0f);
    }

    public void DrawPosition(Vector3 pos, float overrideScale) {
      DrawPosition(pos, Color.white, 0f, overrideScale);
    }

    public void DrawRect(Transform frame, Rect rect) {
      PushMatrix();

      this.matrix = frame.localToWorldMatrix;
      DrawLine(rect.Corner00(), rect.Corner01());
      DrawLine(rect.Corner01(), rect.Corner11());
      DrawLine(rect.Corner11(), rect.Corner10());
      DrawLine(rect.Corner10(), rect.Corner00());

      PopMatrix();
    }

    public void ClearAllGizmos() {
      _operations.Clear();
      _matrices.Clear();
      _colors.Clear();
      _lines.Clear();
      _wireSpheres.Clear();
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
        int wireSphereIndex = 0;
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

            case OperationType.DrawWireSphere:
              setPass(ref currPass, isUnlit: true);

              GL.Begin(GL.LINES);
              WireSphere wireSphere = _wireSpheres[wireSphereIndex++];
              drawWireSphereNow(wireSphere, ref currPass);
              GL.End();
              break;

            case OperationType.DrawMesh:
              if (GL.wireframe) {
                setPass(ref currPass, isUnlit: true);
              } else {
                setPass(ref currPass, isUnlit: false);
              }

              Graphics.DrawMeshNow(_meshes[meshIndex++],
                                   _currMatrix * _matrices[matrixIndex++]);
              break;
              
            default:
              throw new InvalidOperationException("Unexpected operation type " + type);
          }
        }
      } finally {
        GL.wireframe = false;
      }
    }

    private void drawLineNow(Vector3 a, Vector3 b) {
      GL.Vertex(_currMatrix.MultiplyPoint(a));
      GL.Vertex(_currMatrix.MultiplyPoint(b));
    }

    private void drawWireArcNow(Vector3 center, Vector3 normal,
                                Vector3 radialStartDirection, float radius,
                                float fractionOfCircleToDraw, int numCircleSegments = 16) {
      normal = normal.normalized;
      Vector3 radiusVector = radialStartDirection.normalized * radius;
      Vector3 nextVector;
      int numSegmentsToDraw = (int)(numCircleSegments * fractionOfCircleToDraw);
      Quaternion rotator = Quaternion.AngleAxis(360f / numCircleSegments, normal);
      for (int i = 0; i < numSegmentsToDraw; i++) {
        nextVector = rotator * radiusVector;

        drawLineNow(center + radiusVector, center + nextVector);

        radiusVector = nextVector;
      }
    }

    private void setCurrentPassColorIfNew(Color desiredColor, ref int curPass) {
      if (_currColor != desiredColor) {
        _currColor = desiredColor;
        setPass(ref curPass, isUnlit: true);
      }
    }

    private void drawPlaneSoftenedWireArcNow(Vector3 position,
                                             Vector3 circleNormal,
                                             Vector3 radialStartDirection,
                                             float radius,
                                             Color inFrontOfPlaneColor,
                                             Color behindPlaneColor,
                                             Vector3 planeNormal,
                                             ref int curPass,
                                             float fractionOfCircleToDraw = 1.0f,
                                             int numCircleSegments = 16) {
      var origCurrColor = _currColor;

      var onPlaneDir = planeNormal.Cross(circleNormal);
      var Q = Quaternion.AngleAxis(360f / numCircleSegments, circleNormal);
      var r = radialStartDirection * radius;
      for (int i = 0; i < numCircleSegments + 1; i++) {
        var nextR = Q * r;
        var onPlaneAngle = Infix.Infix.SignedAngle(r, onPlaneDir, circleNormal);
        var nextOnPlaneAngle = Infix.Infix.SignedAngle(nextR, onPlaneDir, circleNormal);
        var front = onPlaneAngle < 0;
        var nextFront = nextOnPlaneAngle < 0;

        if (front != nextFront) {
          var targetColor = Color.Lerp(inFrontOfPlaneColor, behindPlaneColor, 0.5f);
          GL.End();
          setPass(ref curPass, isUnlit: true, desiredCurrColor: targetColor);
          GL.Begin(GL.LINES);
        }
        else if (front) {
          var targetColor = inFrontOfPlaneColor;
          GL.End();
          setPass(ref curPass, isUnlit: true, desiredCurrColor: targetColor);
          GL.Begin(GL.LINES);
        }
        else {
          var targetColor = behindPlaneColor;
          GL.End();
          setPass(ref curPass, isUnlit: true, desiredCurrColor: targetColor);
          GL.Begin(GL.LINES);
        }

        drawLineNow(r, nextR);

        r = nextR;
      }

      _currColor = origCurrColor;
    }

    private void drawWireSphereNow(WireSphere wireSphere,
                                   ref int curPass) {
      var position = wireSphere.pose.position;
      var rotation = wireSphere.pose.rotation;

      var worldPosition = _currMatrix.MultiplyPoint3x4(position);

      var dirToCamera = (Camera.current.transform.position - worldPosition).normalized;
      var dirToCameraInMatrix = _currMatrix.inverse.MultiplyVector(dirToCamera);

      // Wire sphere outline. This is just a wire sphere that faces the camera.
      drawWireArcNow(position, dirToCameraInMatrix, dirToCameraInMatrix.Perpendicular(),
                     wireSphere.radius, 1.0f,
                     numCircleSegments: wireSphere.numSegments);


      var x = rotation * Vector3.right;
      var y = rotation * Vector3.up;
      var z = rotation * Vector3.forward;

      drawPlaneSoftenedWireArcNow(position, y, x, wireSphere.radius,
                                  inFrontOfPlaneColor: _currColor,
                                  behindPlaneColor: _currColor.WithAlpha(_currColor.a * 0.1f),
                                  planeNormal: dirToCameraInMatrix,
                                  curPass: ref curPass,
                                  fractionOfCircleToDraw: 1.0f,
                                  numCircleSegments: wireSphere.numSegments);
      drawPlaneSoftenedWireArcNow(position, z, y, wireSphere.radius,
                                  inFrontOfPlaneColor: _currColor,
                                  behindPlaneColor: _currColor.WithAlpha(_currColor.a * 0.1f),
                                  planeNormal: dirToCameraInMatrix,
                                  curPass: ref curPass,
                                  fractionOfCircleToDraw: 1.0f,
                                  numCircleSegments: wireSphere.numSegments);
      drawPlaneSoftenedWireArcNow(position, x, z, wireSphere.radius,
                                  inFrontOfPlaneColor: _currColor,
                                  behindPlaneColor: _currColor.WithAlpha(_currColor.a * 0.1f),
                                  planeNormal: dirToCameraInMatrix,
                                  curPass: ref curPass,
                                  fractionOfCircleToDraw: 1.0f,
                                  numCircleSegments: wireSphere.numSegments);
    }

    private void setPass(ref int currPass, bool isUnlit, Color? desiredCurrColor = null) {

      bool needToUpdateColor = false;
      if (desiredCurrColor.HasValue) {
        needToUpdateColor = _currColor != desiredCurrColor.Value;
        _currColor = desiredCurrColor.Value;
      }

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

      if (currPass != newPass || needToUpdateColor) {
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
      DrawWireSphere,
      DrawMesh
    }

    private struct Line {
      public Vector3 a, b;

      public Line(Vector3 a, Vector3 b) {
        this.a = a;
        this.b = b;
      }
    }

    private struct WireSphere {
      public Pose  pose;
      public float radius;
      public int numSegments;
    }
  }

  public static class RuntimeGizmoExtensions {

    public static void DrawPose(this RuntimeGizmos.RuntimeGizmoDrawer drawer,
                                Pose pose, float radius = 0.10f,
                                bool drawCube = false) {
      drawer.PushMatrix();

      drawer.matrix = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);

      var origColor = drawer.color;

      //drawer.DrawWireSphere(Vector3.zero, radius);
      if (drawCube) {
        drawer.DrawCube(Vector3.zero, Vector3.one * radius * 0.3f);
      }
      drawer.DrawPosition(Vector3.zero, radius * 2);

      drawer.color = origColor;

      drawer.PopMatrix();
    }

    public static void DrawRay(this RuntimeGizmos.RuntimeGizmoDrawer drawer,
                               Vector3 position, Vector3 direction) {
      drawer.DrawLine(position, position + direction);
    }

  }

}
