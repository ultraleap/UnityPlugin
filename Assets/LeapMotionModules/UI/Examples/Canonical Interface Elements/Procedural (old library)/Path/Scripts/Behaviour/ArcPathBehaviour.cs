using UnityEngine;
using Leap.Unity.Attributes;

namespace Procedural.DynamicPath {

  public class ArcPathBehaviour : PathBehaviourBase {

    [SerializeField]
    private AnchorPoint _anchorPoint;

    [MinValue(0)]
    [SerializeField]
    private float _radius = 1;

    [SerializeField]
    private float _startAngle;

    [SerializeField]
    private float _endAngle;

    public float Radius {
      get {
        return _radius;
      }
      set {
        _radius = value;
      }
    }

    public float StartAngle {
      get {
        return _startAngle;
      }
      set {
        _startAngle = value;
      }
    }

    public float EndAngle {
      get {
        return _endAngle;
      }
      set {
        _endAngle = value;
      }
    }

    public override IPath Path {
      get {
        Vector3 center = Vector3.zero;
        if (_anchorPoint == AnchorPoint.Edge) {
          center = Vector3.left * _radius;
        }
        return new ArcPath(Matrix4x4.TRS(center, Quaternion.identity, Vector3.one), _radius, Mathf.Deg2Rad * _startAngle, Mathf.Deg2Rad * _endAngle);
      }
    }

    public Vector3 LocalCenter {
      get {
        return _anchorPoint == AnchorPoint.Center ? Vector3.zero : Vector3.left * _radius;
      }
    }

    public enum AnchorPoint {
      Center,
      Edge
    }
  }
}
