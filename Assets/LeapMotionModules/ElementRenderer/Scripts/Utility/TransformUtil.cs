using UnityEngine;

public static class TransformUtil {

  public static Quaternion TransformRotation(this Transform transform, Quaternion rotation) {
    return transform.rotation * rotation;
  }

  public static Quaternion InverseTransformRotation(this Transform transform, Quaternion rotation) {
    return Quaternion.Inverse(transform.rotation) * rotation;
  }

  public static void SetLocalX(this Transform transform, float localX) {
    transform.setLocalAxis(localX, 0);
  }

  public static void SetLocalY(this Transform transform, float localY) {
    transform.setLocalAxis(localY, 1);
  }

  public static void SetLocalZ(this Transform transform, float localZ) {
    transform.setLocalAxis(localZ, 2);
  }

  /// <summary>
  /// Returns a hash code of the entire transform hierarchy, changing 
  /// any of the following should result in a different hash code:
  ///  - position/rotation/scale of any transform
  ///  - creation/deletion/reordering of any transform
  ///  - turning a transform into a rect transform or back
  /// </summary>
  public static int GetHierarchyHash(Transform root) {
    int hash = 17;

    combineHash(ref hash, GetDataHashCode(root));
    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      combineHash(ref hash, GetHierarchyHash(root.GetChild(i)));
    }

    return hash;
  }

  /// <summary>
  /// Returns a hash of the Transform in addition to it's data.
  /// Changing either the position, rotation, or scale of a 
  /// transform will result in a different hash.  Two transforms
  /// with the same position rotation and scale will not have
  /// the same hash!
  /// </summary>
  public static int GetDataHashCode(this Transform transform) {
    int hash = 17;
    combineHash(ref hash, transform);
    combineHash(ref hash, transform.localPosition);
    combineHash(ref hash, transform.localRotation);
    combineHash(ref hash, transform.localScale);

    if (transform is RectTransform) {
      RectTransform rectTransform = transform as RectTransform;
      combineHash(ref hash, rectTransform.rect);
    }
    return hash;
  }

  private static void combineHash<T>(ref int hash, T combineWith) {
    hash = hash * 31 + combineWith.GetHashCode();
  }

  private static void setLocalAxis(this Transform transform, float value, int axis) {
    Vector3 local = transform.localPosition;
    local[axis] = value;
    transform.localPosition = local;
  }
}
