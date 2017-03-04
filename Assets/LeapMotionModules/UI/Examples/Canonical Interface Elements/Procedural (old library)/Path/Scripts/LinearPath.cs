using UnityEngine;

namespace Procedural.DynamicPath {

  public struct LinearPath : IPath, IHasDirection {
    private Vector3 _start, _end;

    private Vector3 _direction;
    private float _length;

    public Vector3 start {
      get {
        return _start;
      }
      set {
        _start = value;
        refresh();
      }
    }

    public Vector3 end {
      get {
        return _end;
      }
      set {
        _end = value;
        refresh();
      }
    }

    public float Length {
      get {
        return _length;
      }
    }

    public LinearPath(Vector3 start, Vector3 end) {
      _start = start;
      _end = end;
      _direction = _end - _start;
      _length = _direction.magnitude;
      _direction /= _length;
    }

    public Vector3 GetPosition(float distance) {
      return _start + _direction * distance;
    }

    public Vector3 GetDirection(float distance) {
      return _direction;
    }

    public void GetPositionAndDirection(float distance, out Vector3 position, out Vector3 direction) {
      position = GetPosition(distance);
      direction = _direction;
    }

    private void refresh() {
      _direction = _end - _start;
      _length = _direction.magnitude;
      _direction /= _length;
    }
  }
}
