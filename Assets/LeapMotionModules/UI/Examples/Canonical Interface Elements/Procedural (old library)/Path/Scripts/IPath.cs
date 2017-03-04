using UnityEngine;

namespace Procedural.DynamicPath {

  public interface IPath {
    float Length { get; }

    Vector3 GetPosition(float distance);
  }

  public interface IHasDirection {
    void GetPositionAndDirection(float distance, out Vector3 position, out Vector3 direction);
    Vector3 GetDirection(float distance);
  }

  public interface IHasOrientation {
    Quaternion GetOrientation(float distance);
  }
}
