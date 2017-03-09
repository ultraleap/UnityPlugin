using System.Collections.Generic;
using UnityEngine;

public static class HashUtil {

  /// <summary>
  /// Returns a hash code of the entire transform hierarchy, changing 
  /// any of the following should result in a different hash code:
  ///  - position/rotation/scale of any transform
  ///  - creation/deletion/reordering of any transform
  ///  - turning a transform into a rect transform or back
  ///  - enabling or disabling ANY behavior
  /// </summary>
  private static List<Behaviour> _behaviourCache = new List<Behaviour>();
  public static int GetHierarchyHash(Transform root) {
    int hash = 17;

    Combine(ref hash, GetDataHashCode(root));
    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      Combine(ref hash, GetHierarchyHash(root.GetChild(i)));
    }

    root.GetComponents(_behaviourCache);
    for (int i = 0; i < _behaviourCache.Count; i++) {
      Combine(ref hash, _behaviourCache[i].enabled);
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
    Combine(ref hash, transform);
    Combine(ref hash, transform.gameObject.activeSelf);
    Combine(ref hash, transform.localPosition);
    Combine(ref hash, transform.localRotation);
    Combine(ref hash, transform.localScale);

    if (transform is RectTransform) {
      RectTransform rectTransform = transform as RectTransform;
      Combine(ref hash, rectTransform.rect);
    }
    return hash;
  }

  public static void Combine<T>(ref int hash, T combineWith) {
    hash = hash * 31 + combineWith.GetHashCode();
  }

  public static void CombineSequence<T>(ref int hash, List<T> sequence) {
    for (int i = 0; i < sequence.Count; i++) {
      Combine(ref hash, sequence[i]);
    }
  }
}
