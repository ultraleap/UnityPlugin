using UnityEngine;

namespace Procedural.DynamicPath {

  public struct ArcPath : IPath, IHasDirection {

    private Matrix4x4 _transform;
    private float _radius;
    private float _startAngle, _endAngle; //in radians

    public float Length {
      get {
        return (_endAngle - _startAngle) * _radius;
      }
    }

    public ArcPath(Matrix4x4 transform, float radius, float startAngle, float endAngle) {
      _transform = transform * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * radius);
      _radius = radius;
      _startAngle = startAngle;
      _endAngle = endAngle;
    }

    public Vector3 GetPosition(float distance) {
      float dx, dy;
      getDxDy(distance, out dx, out dy);

      return _transform.MultiplyPoint3x4(new Vector3(dx, dy, 0));
    }

    public Vector3 GetDirection(float distance) {
      float dx, dy;
      getDxDy(distance, out dx, out dy);

      return _transform.MultiplyVector(new Vector3(dy, -dx, 0));
    }

    public void GetPositionAndDirection(float distance, out Vector3 position, out Vector3 direction) {
      float dx, dy;
      getDxDy(distance, out dx, out dy);

      position = _transform.MultiplyPoint3x4(new Vector3(dx, dy, 0));
      direction = _transform.MultiplyVector(new Vector3(dy, -dx, 0));
    }

    private void getDxDy(float distance, out float dx, out float dy) {
      float percent = distance / Length;
      float angle = Mathf.Lerp(_startAngle, _endAngle, percent);

      dx = Mathf.Cos(angle);
      dy = Mathf.Sin(angle);
    }
  }
}
