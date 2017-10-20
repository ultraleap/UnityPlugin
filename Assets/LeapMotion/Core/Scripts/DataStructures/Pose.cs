using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// A position and rotation.
  /// </summary>
  [System.Serializable]
  public struct Pose {

    public Vector3    position;
    public Quaternion rotation;

    public Pose(Vector3 position, Quaternion rotation) {
      this.position = position;
      this.rotation = rotation;
    }

    public static Pose identity {
      get { return new Pose(); }
    }

    /// <summary>
    /// Returns a delta Pose such that (other Pose).Then(delta Pose) == this Pose.
    /// </summary>
    public Pose From(Pose other) {
      return new Pose(this.position - other.position,
                      this.rotation * Quaternion.Inverse(other.rotation));
    }

    /// <summary>
    /// Accumulates the provided Pose onto this Pose. For example:
    /// PoseB.Then(PoseA.From(PoseB)) == PoseA.
    /// 
    /// Position and rotation are handled independently; Pose rotations do not affect
    /// Pose positions.
    /// </summary>
    public Pose Then(Pose other) {
      return new Pose(this.position + other.position,
                      other.rotation * this.rotation);
    }

    public bool ApproxEquals(Pose other) {
      return position.ApproxEquals(other.position) && rotation.ApproxEquals(other.rotation);
    }

    /// <summary>
    /// Returns a pose interpolated (Lerp for position, Slerp for rotation)
    /// between a and b by t from 0 to 1. This method clamps t between 0 and 1; if
    /// extrapolation is desired, see Extrapolate.
    /// </summary>
    public static Pose Interpolate(Pose a, Pose b, float t) {
      if (t >= 1f) return b;
      if (t <= 0f) return a;
      return new Pose(Vector3.Lerp(a.position, b.position, t),
                      Quaternion.Slerp(a.rotation, b.rotation, t));
    }

    /// <summary>
    /// As Interpolate, but doesn't clamp t between 0 and 1. Values above one extrapolate
    /// forwards beyond b, while values less than zero extrapolate backwards past a.
    /// </summary>
    public static Pose Extrapolate(Pose a, Pose b, float t) {
      return new Pose(Vector3.LerpUnclamped(a.position, b.position, t),
                      Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));
    }

    /// <summary>
    /// As Extrapolate, but extrapolates using time values for a and b, and a target time
    /// at which to determine the extrapolated pose.
    /// </summary>
    public static Pose TimedExtrapolate(Pose a, float aTime, Pose b, float bTime,
                                        float extrapolateTime) {
      return Extrapolate(a, b, extrapolateTime.MapUnclamped(aTime, bTime, 0f, 1f));
    }

    public override string ToString() {
      return "[Pose | Position: " + this.position.ToString() + ", Rotation: " + this.rotation.ToString() + "]";
    }

    public override bool Equals(object obj) {
      if (!(obj is Pose)) return false;
      else return this.Equals((Pose)obj);
    }
    public bool Equals(Pose other) {
      return other.position == this.position && other.rotation == this.rotation;
    }

    public override int GetHashCode() {
      return this.position.GetHashCode() ^ this.rotation.GetHashCode() * 7;
    }

    public static bool operator ==(Pose a, Pose b) {
      return a.Equals(b);
    }

    public static bool operator !=(Pose a, Pose b) {
      return !(a.Equals(b));
    }

  }

  public static class PoseExtensions {

    /// <summary>
    /// Creates a Pose using the transform's localPosition and localRotation.
    /// </summary>
    public static Pose ToLocalPose(this Transform t) {
      return new Pose(t.localPosition, t.localRotation);
    }

    /// <summary>
    /// Creates a Pose using the transform's position and rotation.
    /// </summary>
    public static Pose ToWorldPose(this Transform t) {
      return new Pose(t.position, t.rotation);
    }

    /// <summary>
    /// Sets the localPosition and localRotation of this transform to the argument pose's
    /// position and rotation.
    /// </summary>
    public static void SetLocalPose(this Transform t, Pose localPose) {
      t.localPosition = localPose.position;
      t.localRotation = localPose.rotation;
    }

    /// <summary>
    /// Sets the position and rotation of this transform to the argument pose's
    /// position and rotation.
    /// </summary>
    public static void SetWorldPose(this Transform t, Pose worldPose) {
      t.position = worldPose.position;
      t.rotation = worldPose.rotation;
    }

    /// <summary>
    /// Returns the pose (position and rotation) described by a Matrix4x4.
    /// </summary>
    public static Pose GetPose(this Matrix4x4 m) {
      return new Pose(m.GetColumn(3),
                      m.GetColumn(2) == m.GetColumn(1) ? Quaternion.identity
                                                       : Quaternion.LookRotation(
                                                           m.GetColumn(2),
                                                           m.GetColumn(1)));
    }

    public static Vector3 GetVector3(this Matrix4x4 m) { return m.GetColumn(3); }

    public static Quaternion GetQuaternion(this Matrix4x4 m) {
      if (m.GetColumn(2) == m.GetColumn(1)) { return Quaternion.identity; }
      return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    public const float EPSILON = 0.0001f;

    public static bool ApproxEquals(this Vector3 v0, Vector3 v1) {
      return (v0 - v1).magnitude < EPSILON;
    }

    public static bool ApproxEquals(this Quaternion q0, Quaternion q1) {
      return (q0.ToAngleAxisVector() - q1.ToAngleAxisVector()).magnitude < EPSILON;
    }

  }

}
