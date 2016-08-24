using UnityEngine;
using System.Collections;
using Leap.Unity;

public class ShoulderTurnBehavior : MonoBehaviour {

  private const string TWIST_LEFT_LABEL = "chest_twist_left";
  private const string TWIST_RIGHT_LABEL = "chest_twist_right";

  /// <summary>
  /// Used for positonal and rotational refrence.
  /// </summary>
  /// <remarks>
  /// Should be the neck with +z = forward & +x = right
  /// </remarks>
  public Transform NeckReferenceTransform;
  public Transform Target;
  public float Smoothing;

  private Animator m_animator;

  private float m_lastNormalYRotation;

  // Use this for initialization
  void Start() {
    m_animator = GetComponent<Animator>();
    NeckReferenceTransform = m_animator.GetBoneTransform(HumanBodyBones.Neck);
    GameObject markerPrefab = Resources.Load("AxisTripod") as GameObject;
    Target = GameObject.Instantiate(markerPrefab).transform;
    Target.name = transform.name + "_ChestReferenceMarker";
    Target.parent = GameObject.FindObjectOfType<LeapVRCameraControl>().transform;
    Target.localPosition = new Vector3(0, 0, 2);
  }

  // Update is called once per frame 
  void LateUpdate() {
    rotateChestToFollow(Target);
  }

  private void rotateChestToFollow(Transform target) {
    const float MIN_LIMIT = -40.0f;
    const float MAX_LIMIT = 40.0f;
    Vector3 toTarget = (target.position - NeckReferenceTransform.position).normalized;
    Quaternion toTargetRotation = Quaternion.FromToRotation(NeckReferenceTransform.forward, toTarget);
    float aboutYRotation = NeckReferenceTransform.rotation.eulerAngles.y + toTargetRotation.eulerAngles.y;

    while (aboutYRotation > 180.0f) { aboutYRotation -= 360.0f; }

    float normalYRotation = (aboutYRotation - MIN_LIMIT) / (MAX_LIMIT - MIN_LIMIT);
    // float smoothedNormalYRotation = m_lastNormalYRotation + (normalYRotation * (1.0f - Smoothing));
    //m_lastNormalYRotation = smoothedNormalYRotation;

    setChestTwist(normalYRotation);
  }

  private void setChestTwist(float normalizedTwist) {
    normalizedTwist = Mathf.Clamp01(normalizedTwist);
    float rightVal = normalizedTwist;
    float leftVal = 1.0f - normalizedTwist;

    m_animator.SetFloat(TWIST_RIGHT_LABEL, rightVal);
    m_animator.SetFloat(TWIST_LEFT_LABEL, leftVal);
  }
}
