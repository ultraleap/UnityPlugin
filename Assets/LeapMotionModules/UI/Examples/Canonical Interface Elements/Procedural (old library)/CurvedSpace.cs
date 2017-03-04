using UnityEngine;
using Leap.Unity.RuntimeGizmos;

public class CurvedSpace : MonoBehaviour, IRuntimeGizmoComponent {

  [SerializeField]
  private bool _showGizmos = false;

  public Vector3 RectToLocal(Vector3 rect, float radius = float.NaN, bool useRadius = true) {
    if (!useRadius || float.IsNaN(radius)) {
      radius = rect.z;
    }
    float theta = rect.x / radius;
    float dx = Mathf.Sin(theta) * rect.z;
    float dz = Mathf.Cos(theta) * rect.z;
    return new Vector3(dx, rect.y, dz);
  }

  public Quaternion RotationToLocalByRect(Quaternion rotation, Vector3 rect, float radius = float.NaN, bool useRadius = true) {
    if (!useRadius || float.IsNaN(radius)) {
      radius = rect.z;
    }
    return Quaternion.Euler(0, Mathf.Rad2Deg * rect.x / radius, 0) * rotation;
  }

  public Quaternion RotationToWorldByRect(Quaternion rotation, Vector3 rect, float radius = float.NaN, bool useRadius = true) {
    if (!useRadius || float.IsNaN(radius)) {
      radius = rect.z;
    }
    return transform.rotation * RotationToLocalByRect(rotation, rect, radius);
  }

  public Quaternion RotationToLocalByLocal(Quaternion rotation, Vector3 local) {
    return Quaternion.Euler(0, Mathf.Rad2Deg * Mathf.Atan2(local.x, local.z), 0) * rotation;
  }

  public Quaternion RotationToWorldByLocal(Quaternion rotation, Vector3 local) {
    return RotationToLocalByLocal(rotation, local) * transform.rotation;
  }

  public Quaternion RotationToWorldByWorld(Quaternion rotation, Vector3 world) {
    return RotationToWorldByLocal(rotation, transform.InverseTransformPoint(world));
  }

  public Vector3 LocalToRect(Vector3 local, float radius = float.NaN, bool useRadius = true) {
    float z = Mathf.Sqrt(local.x * local.x + local.z * local.z);

    if (!useRadius || float.IsNaN(radius)) {
      radius = z;
    }

    float y = local.y;
    float x = Mathf.Atan2(local.x, local.z) * radius;
    return new Vector3(x, y, z);
  }

  public Vector3 RectToWorld(Vector3 rect, float radius = float.NaN, bool useRadius = true) {
    return transform.TransformPoint(RectToLocal(rect, radius, useRadius));
  }

  public Vector3 WorldToRect(Vector3 world, float radius = float.NaN, bool useRadius = true) {
    return LocalToRect(transform.InverseTransformPoint(world), radius, useRadius);
  }

  public Vector3 GetLocalNormalFromRect(Vector3 rect, float radius = float.NaN, bool useRadius = true) {
    if (!useRadius || float.IsNaN(radius)) {
      radius = rect.z;
    }
    return Quaternion.Euler(0, Mathf.Rad2Deg * rect.x / radius, 0) * Vector3.forward;
  }

  public float SignedLocalDistanceToRadius(Vector3 local, float radius) {
    float dist = Mathf.Sqrt(local.x * local.x + local.z * local.z);
    return radius - dist;
  }

  public float SignedWorldDistanceToRadius(Vector3 world, float radius) {
    return SignedLocalDistanceToRadius(transform.InverseTransformPoint(world), radius);
  }

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (_showGizmos) {
      for (float r = 0.1f; r <= 1; r += 0.25f) {
        drawer.DrawWireCirlce(transform.position, transform.up, r);
      }
    }
  }
}
