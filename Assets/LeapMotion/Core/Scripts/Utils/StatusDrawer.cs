using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Leap.Unity {
  public class StatusDrawer : MonoBehaviour {

    // The Runtime Gizmo Manager isn't working and HyperMegaLines is being unreliable.
    // So in times like these, you just make something dead simple that works.

    // Static Singleton Interface
    private static StatusDrawer _instance;
    public static StatusDrawer instance {
      get {
        if (_instance == null) {
          _instance = FindObjectOfType<StatusDrawer>();
        }
        if (_instance == null) {
          Debug.LogError("No StatusUpdater object exists in the scene, "
            + "but something is trying to access it.");
          _instance = new GameObject("Status Drawer").AddComponent<StatusDrawer>();
        }
        return _instance;
      }
    }

    public static void DrawSphere(Vector3 position, float radius = 0.0165f, int matIdx = 0) {
      instance._spheres[matIdx].Add(new SphereCommand {
        center = position, radius = radius
      });
    }

    public static void DrawLine(Vector3 start, Vector3 end, float thickness = 0.001f, int matIdx = 0) {
      instance._lines[matIdx].Add(new LineCommand {
        start = start, end = end, thickness = thickness
      });
    }

    // Mere Mortal Implementation
    public Mesh sphereMesh;
    public Mesh cylinderMesh;
    public Material[] statusIndicatorMaterials;

    protected struct SphereCommand {
      public Vector3 center; public float radius;
    }
    protected struct LineCommand {
      public Vector3 start; public Vector3 end; public float thickness;
    }

    protected List<SphereCommand>[] _spheres; 
    protected List<  LineCommand>[] _lines;
    protected Matrix4x4           [] _matrices;

    protected void OnEnable() {
      _matrices = new Matrix4x4[1023];
      _spheres  = new List<SphereCommand>[statusIndicatorMaterials.Length];
      _lines    = new List<LineCommand>  [statusIndicatorMaterials.Length];
      for (int matIdx = 0; matIdx < statusIndicatorMaterials.Length; matIdx++) {
        _spheres[matIdx] = new List<SphereCommand>();
        _lines  [matIdx] = new List<  LineCommand>();
      }
    }

    private void LateUpdate() {
      for(int matIdx = 0; matIdx < statusIndicatorMaterials.Length; matIdx++) {
        { // Draw Spheres
          int meshCount = _spheres[matIdx].Count;
          for (int i = 0; i < meshCount; i++) {
            SphereCommand cmd = _spheres[matIdx][i];
            _matrices[i] = Matrix4x4.TRS(cmd.center, Quaternion.identity, Vector3.one * cmd.radius);
          }

          Graphics.DrawMeshInstanced(sphereMesh, 0, statusIndicatorMaterials[matIdx],
            _matrices, meshCount, null, ShadowCastingMode.Off, false, gameObject.layer);
        }

        { // Draw "Lines" (Cylinders)
          int meshCount = _lines[matIdx].Count;
          for (int i = 0; i < meshCount; i++) {
            LineCommand cmd = _lines[matIdx][i];
            _matrices[i] = Matrix4x4.TRS(
              (cmd.start + cmd.end)*0.5f, 
              Quaternion.FromToRotation(Vector3.up, cmd.end-cmd.start), 
              new Vector3(cmd.thickness, Vector3.Distance(cmd.start, cmd.end)*0.5f,cmd.thickness));
          }

          Graphics.DrawMeshInstanced(cylinderMesh, 0, statusIndicatorMaterials[matIdx],
            _matrices, meshCount, null, ShadowCastingMode.Off, false, gameObject.layer);
        }
      }
    }

    private void FixedUpdate() {
      for (int matIdx = 0; matIdx < statusIndicatorMaterials.Length; matIdx++) {
        _spheres[matIdx].Clear();
        _lines  [matIdx].Clear();
      }
    }
  }
}
