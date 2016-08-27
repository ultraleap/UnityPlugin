using UnityEngine;
using System.Collections;
using Leap.Unity;


[RequireComponent(typeof(Animator))]
public class SpineFollowTargetBehavior : MonoBehaviour {

  private enum Direction {
    LEFT,
    RIGHT,
    FORWARD,
    BACK
  }

  private const string SPINE_LABEL = "spine";
  private const string FORWARD_LABEL = "forward";
  private const string BACK_LABEL = "back";
  private const string LEFT_LABEL = "left";
  private const string RIGHT_LABEL = "right";

  public Transform ReferenceTransform;
  public Transform Target;

  private Animator m_animator;
  private Transform m_spineRootTransform;

  // Use this for initialization
  void Start() {
    m_animator = GetComponent<Animator>();
    Target = GameObject.FindObjectOfType<LeapVRCameraControl>().transform;
    GameObject markerPrefab = Resources.Load("AxisTripod") as GameObject;
    ReferenceTransform = GameObject.Instantiate(markerPrefab).transform;
    ReferenceTransform.name = transform.name + "_SpineReferenceMarker";
    ReferenceTransform.parent = transform;
    ReferenceTransform.localPosition = new Vector3(0, 0, 2);
    m_spineRootTransform = getSpineRootTransform();
  }

  // Update is called once per frame
  void LateUpdate() {
    rotateSpineToFollow(Target);
  }

  private Transform getSpineRootTransform() {
    return m_animator.GetBoneTransform(HumanBodyBones.Spine);
  }

  private void rotateSpineToFollow(Transform target) {
    const float MIN_LIMIT = -40.0f; // In degrees from 0 (straight up)
    const float MAX_LIMIT = 40.0f;
    Vector3 ToTarget = (transform.parent.InverseTransformPoint( target.position) - transform.parent.InverseTransformPoint( m_spineRootTransform.position)).normalized;
    Quaternion toTargetRotation = Quaternion.FromToRotation(ReferenceTransform.up, ToTarget);
    float aboutZRotation = toTargetRotation.eulerAngles.z;
    float aboutXRotation = toTargetRotation.eulerAngles.x;

    while (aboutZRotation > 180.0f) { aboutZRotation -= 360.0f; }
    while (aboutXRotation > 180.0f) { aboutXRotation -= 360.0f; }

    float normalZRotation = (aboutZRotation - MIN_LIMIT) / (MAX_LIMIT - MIN_LIMIT);
    float normalXRotation = (aboutXRotation - MIN_LIMIT) / (MAX_LIMIT - MIN_LIMIT);

    setSpineMuscles(normalXRotation, normalZRotation);
  }

  private void setSpineMuscles(float forwardBackwardValue, float leftRightValue) {
    forwardBackwardValue = Mathf.Clamp01(forwardBackwardValue);
    leftRightValue = Mathf.Clamp01(leftRightValue);
    setSpineMuscle(Direction.LEFT, Direction.RIGHT, leftRightValue);
    setSpineMuscle(Direction.FORWARD, Direction.BACK, forwardBackwardValue);
  }

  private void setSpineMuscle(Direction maxDirection, Direction minDirection, float normalValue) {
    normalValue = Mathf.Clamp01(normalValue);
    float maxVal = normalValue;
    float minVal = 1.0f - normalValue;

    m_animator.SetFloat(propertyName(maxDirection), maxVal);
    m_animator.SetFloat(propertyName(minDirection), minVal);
  }

  // I should have made this a dictionary.
  private string propertyName(Direction direction) {
    string propertyString = SPINE_LABEL;

    propertyString += "_";

    switch (direction) {
      case Direction.BACK:
        propertyString += BACK_LABEL;
        break;
      case Direction.FORWARD:
        propertyString += FORWARD_LABEL;
        break;
      case Direction.LEFT:
        propertyString += LEFT_LABEL;
        break;
      case Direction.RIGHT:
        propertyString += RIGHT_LABEL;
        break;
    }

    return propertyString;
  }
}
