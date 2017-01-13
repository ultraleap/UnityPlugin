using UnityEngine;

public static class ValueMappingExtensions {

  /// <summary>
  /// Maps the value between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax.
  /// </summary>
  public static float Map(this float value, float valueMin, float valueMax, float resultMin, float resultMax) {
    if (valueMin == valueMax) return resultMin;
    return Mathf.Lerp(resultMin, resultMax, ((value - valueMin) / (valueMax - valueMin)));
  }

  public static Vector2 Map(this Vector2 value, float valueMin, float valueMax, float resultMin, float resultMax) {
    return new Vector2(value.x.Map(valueMin, valueMax, resultMin, resultMax),
                       value.y.Map(valueMin, valueMax, resultMin, resultMax));
  }

  public static Vector3 Map(this Vector3 value, float valueMin, float valueMax, float resultMin, float resultMax) {
    return new Vector3(value.x.Map(valueMin, valueMax, resultMin, resultMax),
                       value.y.Map(valueMin, valueMax, resultMin, resultMax),
                       value.z.Map(valueMin, valueMax, resultMin, resultMax));
  }

  /// <summary>
  /// Clamps the value between 0 and 1.
  /// </summary>
  public static float Clamp01(this float value) {
    return Mathf.Clamp01(value);
  }

  /// <summary>
  /// Returns a new Vector3 via component-wise multiplication.
  /// </summary>
  public static Vector3 CompMul(this Vector3 A, Vector3 B) {
    return new Vector3(A.x * B.x, A.y * B.y, A.z * B.z);
  }

  /// <summary>
  /// Returns a new Vector3 via component-wise division.
  /// </summary>
  public static Vector3 CompDiv(this Vector3 A, Vector3 B) {
    return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
  }

  /// <summary>
  /// Returns a new Vector2 via component-wise multiplication.
  /// </summary>
  public static Vector2 CompMul(this Vector2 A, Vector2 B) {
    return new Vector2(A.x * B.x, A.y * B.y);
  }

  /// <summary>
  /// Returns a new Vector3 via component-wise division.
  /// </summary>
  public static Vector2 CompDiv(this Vector2 A, Vector2 B) {
    return new Vector2(A.x / B.x, A.y / B.y);
  }

}