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

  private Transform head;
  public Transform CamTarg;

  // Use this for initialization
  void Start() {
    m_animator = GetComponent<Animator>();
    NeckReferenceTransform = m_animator.GetBoneTransform(HumanBodyBones.Neck);
    GameObject markerPrefab = Resources.Load("AxisTripod") as GameObject;
    Target = GameObject.Instantiate(markerPrefab).transform;
    Target.name = transform.name + "_ChestReferenceMarker";
    Target.parent = GameObject.FindObjectOfType<LeapVRCameraControl>().transform;
    Target.localPosition = new Vector3(0, 0, 2);

    head = m_animator.GetBoneTransform(HumanBodyBones.Head);
  }
  // Update is called once per frame 
  void LateUpdate() {
    Vector3 flattenedTargetPosition = new Vector3(Target.position.x, NeckReferenceTransform.position.y, Target.position.z);
    Target.position = flattenedTargetPosition;
    rotateChestToFollow(Target);
    rollHead();


    //Head Rotation
    //Vector3 relativePos = CamTarg.position - head.transform.position;
    //Quaternion rotation = Quaternion.LookRotation(relativePos, CamTarg.up);
    //head.transform.rotation = rotation;
  }
  private void rollHead() {
    const float MIN_LIMIT = -40.0f;
    const float MAX_LIMIT = 40.0f;
    float targetRotationZ = CamTarg.parent.localEulerAngles.z;
    //Debug.Log(targetRotationZ);
    while (targetRotationZ > 180.0f) { targetRotationZ -= 360.0f; }
    float normalZRotation = (targetRotationZ - MIN_LIMIT) / (MAX_LIMIT - MIN_LIMIT);
    setHeadRoll(normalZRotation);
  }
  private void setHeadRoll(float normalizedRoll) {
    normalizedRoll = Mathf.Clamp01(normalizedRoll);
    float rightVal = normalizedRoll;
    float leftVal = 1.0f - normalizedRoll;
    m_animator.SetFloat("head_tilt_left", leftVal);
    m_animator.SetFloat("head_tilt_right", rightVal);
    m_animator.SetFloat("neck_tilt_left", leftVal * .1f);
    m_animator.SetFloat("neck_tilt_right", rightVal * .1f);
  }

  private void rotateChestToFollow(Transform target) {
    const float MIN_LIMIT = -40.0f;
    const float MAX_LIMIT = 40.0f;
    Vector3 toTarget = (transform.root.InverseTransformPoint(target.position) - transform.root.InverseTransformPoint(NeckReferenceTransform.position)).normalized;
    Quaternion toTargetRotation = Quaternion.FromToRotation(NeckReferenceTransform.forward, toTarget);
    float aboutYRotation = NeckReferenceTransform.rotation.eulerAngles.y + toTargetRotation.eulerAngles.y;

    while (aboutYRotation > 180.0f) { aboutYRotation -= 360.0f; }

    float normalYRotation = (aboutYRotation - MIN_LIMIT) / (MAX_LIMIT - MIN_LIMIT);
    //float smoothedNormalYRotation = m_lastNormalYRotation + (normalYRotation * (1.0f - Smoothing));
    //m_lastNormalYRotation = smoothedNormalYRotation;

    setChestTwist(normalYRotation);
  }

  private void setChestTwist(float normalizedTwist) {
    normalizedTwist = Mathf.Clamp01(normalizedTwist);
    float rightVal = normalizedTwist;
    float leftVal = 1.0f - normalizedTwist;
    float currentRightVal = m_animator.GetFloat(TWIST_RIGHT_LABEL);
    float currentLeftVal = m_animator.GetFloat(TWIST_LEFT_LABEL);
    m_animator.SetFloat(TWIST_RIGHT_LABEL, Mathf.Lerp(currentRightVal, rightVal, .05f));
    m_animator.SetFloat(TWIST_LEFT_LABEL, Mathf.Lerp(currentLeftVal, leftVal, .05f));
    //m_animator.SetFloat(TWIST_RIGHT_LABEL, rightVal);
    //m_animator.SetFloat(TWIST_LEFT_LABEL, leftVal);
  }

  public void OnAnimatorIK(int layerIndex) {
    m_animator.SetLookAtWeight(1.0f);
    m_animator.SetLookAtPosition(CamTarg.position);
  }
}
