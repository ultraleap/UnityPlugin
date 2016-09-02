using UnityEngine;
using System.Collections;
using Leap.Unity;

namespace Leap.Unity {
  public class WristLeapToIKBlend : HandTransitionBehavior {
    [Leap.Unity.Attributes.AutoFind]
    private Animator animator;
    private HandModel handModel;

    private Vector3 startingPalmPosition;
    private Quaternion startingOrientation;
    private Transform palm;

    private Vector3 armDirection;
    private Vector3 PalmPositionAtLateUpdate;
    private Quaternion PalmRotationAtLateUpdate;
    private float positionIKWeight;
    private float rotationIKWeight;
    private float positionIKTargetWeight;
    private float rotationIKTargetWeight;

    private Transform Scapula;
    private Transform Shoulder;
    private Transform Elbow;
    private Transform Neck;
    private float shoulder_up_target_weight;
    private float shoulder_up_weight;
    private float shoulder_forward_weight;
    private float shoulder_forward_target_weight;

    private float shouldersLayerWeight;
    private float shouldersLayerTargetWeight;
    private float spineLayerWeight;
    private float spineLayerTargetWeight;

    private Vector3 UntrackedIKPosition;
    private bool isTracking;
    
    public Chirality Handedness;
    public GameObject MarkerPrefab;
    public Transform ElbowMarker;
    public Transform ElbowIKTarget;
    public float ElbowOffset = -0.5f;
    public Transform RestIKPosition;
    public AnimationCurve DropCurveX;
    public AnimationCurve DropCurveY;
    public AnimationCurve DropCurveZ;
    public float ArmDropDuration = .25f;

    public IKMarkersAssembly m_IKMarkerAssembly;

    protected override void Awake() {
      base.Awake();
      animator = transform.root.GetComponentInChildren<Animator>();
      handModel = transform.GetComponent<HandModel>();
      palm = GetComponent<HandModel>().palm;
      Neck = animator.GetBoneTransform(HumanBodyBones.Neck);
      if(Handedness == Chirality.Left){
        Scapula = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
      }
      if (Handedness == Chirality.Right) {
        Scapula = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
      }
      //MarkerPrefab = Resources.Load("AxisTripod") as GameObject;
      //if (ElbowMarker == null) {
      //  ElbowMarker = GameObject.Instantiate(MarkerPrefab).transform;
      //  ElbowMarker.transform.name = animator.transform.name + "_ElbowMarker" + Handedness.ToString();
      //  ElbowMarker.position = Elbow.position;
      //  ElbowMarker.rotation = Elbow.rotation;
      //}
      //if (ElbowIKTarget == null) {
      //  ElbowIKTarget = GameObject.Instantiate(MarkerPrefab).transform;
      //  ElbowIKTarget.transform.name = animator.transform.name + "_ElbowIKTarget" + Handedness.ToString();
      //}
    }

    public void AssignIKMarkers() {
      if (Handedness == Chirality.Left) {
        Debug.Log(transform.name + " - Left");
        ElbowMarker = m_IKMarkerAssembly.ElbowMarker_L;
        ElbowIKTarget = m_IKMarkerAssembly.ElbowIKTarget_L;
        RestIKPosition = m_IKMarkerAssembly.RestIKPosition_L;
      }
      if (Handedness == Chirality.Right) {
        Debug.Log(transform.name + " - Right");
        ElbowMarker = m_IKMarkerAssembly.ElbowMarker_R;
        ElbowIKTarget = m_IKMarkerAssembly.ElbowIKTarget_R;
        RestIKPosition = m_IKMarkerAssembly.RestIKPosition_R;
      }
      DropCurveX = m_IKMarkerAssembly.DropCurveX;
      DropCurveY = m_IKMarkerAssembly.DropCurveY;
      DropCurveZ = m_IKMarkerAssembly.DropCurveZ;
    }

    protected override void HandFinish() {
      isTracking = false;
      StartCoroutine(LerpToRestPosition(palm.position));
      positionIKTargetWeight = 1;
      rotationIKWeight = 0;
      shoulder_forward_target_weight = 0;
      shoulder_up_target_weight = 0;
      shouldersLayerTargetWeight = 0f;
      spineLayerTargetWeight = 1f;
    }
    protected override void HandReset() {
      isTracking = true;
      StopAllCoroutines();
      positionIKTargetWeight = 1;
      rotationIKWeight = 1;
      shouldersLayerTargetWeight = 1f;
      spineLayerTargetWeight = 1f;
    }

    void Update() {
      //get Arm Directions and set elbow target position
      armDirection = handModel.GetArmDirection();
      Vector3 ElbowTargetPosition = palm.position + (armDirection * ElbowOffset);
      if (Handedness == Chirality.Left ) {
        //Debug.Log("Left palm z: " + PalmPositionAtLateUpdate.z);
        ElbowTargetPosition.x -= PalmPositionAtLateUpdate.z -.2f;
      }
      if (Handedness == Chirality.Right) {
        //Debug.Log("Right palm z: " + PalmPositionAtLateUpdate.z);
        ElbowTargetPosition.x += PalmPositionAtLateUpdate.z -.2f;
      }

      if (Handedness == Chirality.Left && ElbowTargetPosition.x > -.05f) {

        Debug.Log("Left Elbow Inside");
        ElbowTargetPosition.x = -.1f;
      }
      if (Handedness == Chirality.Right && ElbowTargetPosition.x < .05f) {
        Debug.Log("Right Elbow Inside");
        ElbowTargetPosition.x = .1f;
      }
      ElbowIKTarget.position = ElbowTargetPosition;
    }

    void LateUpdate() {
      PalmPositionAtLateUpdate = palm.position;
      PalmRotationAtLateUpdate = palm.rotation;
      ElbowMarker.position = Elbow.position;
      ElbowMarker.rotation = Elbow.rotation;
      shoulder_up_weight = Mathf.Lerp(shoulder_up_weight, shoulder_up_target_weight, .4f);
      shoulder_forward_weight = Mathf.Lerp(shoulder_forward_weight, shoulder_forward_target_weight, .4f);
      positionIKWeight = Mathf.Lerp(positionIKWeight, positionIKTargetWeight, .4f);
      if (Handedness == Chirality.Left) {
        animator.SetLayerWeight(3, shouldersLayerTargetWeight);

      }
      if (Handedness == Chirality.Right) {
        animator.SetLayerWeight(4, shouldersLayerTargetWeight);
      }
      //animator.SetLayerWeight(2, spineLayerTargetWeight);
    }

    public void OnAnimatorIK(int layerIndex) {
      //Debug.Log("IK");
      if (Handedness == Chirality.Left) {
        if (isTracking) {
          animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionIKWeight);
          animator.SetIKPosition(AvatarIKGoal.LeftHand, PalmPositionAtLateUpdate);
          //animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationIKWeight * .75f);
          //animator.SetIKRotation(AvatarIKGoal.LeftHand, PalmRotationAtLateUpdate);
          animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, positionIKWeight);
          animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ElbowIKTarget.position);

          if (ElbowMarker.position.y > Scapula.position.y) {
            shoulder_up_target_weight = (ElbowMarker.position.y - Shoulder.position.y) * 10f;
            animator.SetFloat("shoulder_up_left", shoulder_up_weight);
          }
          else {
            shoulder_up_target_weight = 0.0f;
            animator.SetFloat("shoulder_up_left", shoulder_up_weight);
          }
          if (ElbowMarker.position.x > Shoulder.position.x) {
            shoulder_forward_target_weight = Mathf.Abs(ElbowMarker.position.x - Shoulder.position.x * 10f);
            animator.SetFloat("shoulder_forward_left", shoulder_forward_weight);
          }
          //if (ElbowMarker.position.z > Scapula.position.z) {
          //  shoulder_forward_target_weight = (ElbowMarker.position.z - (Shoulder.position.z + .15f)) * 10f;
          //  animator.SetFloat("shoulder_forward_left", shoulder_forward_weight);
          //}
          else {
            shoulder_forward_target_weight = 0.0f;
            animator.SetFloat("shoulder_forward_left", shoulder_forward_weight);
          }
        }
        else {
          animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionIKWeight);
          animator.SetIKPosition(AvatarIKGoal.LeftHand, UntrackedIKPosition);
          animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationIKWeight);
          animator.SetIKRotation(AvatarIKGoal.LeftHand, PalmRotationAtLateUpdate);
          animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1);
          animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ElbowIKTarget.position);
        }
      }
      if (Handedness == Chirality.Right) {
        if (isTracking) {
          animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionIKWeight);
          animator.SetIKPosition(AvatarIKGoal.RightHand, PalmPositionAtLateUpdate);
          //animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationIKWeight  * .75f);
          //animator.SetIKRotation(AvatarIKGoal.RightHand, PalmRotationAtLateUpdate);
          animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, positionIKWeight);
          animator.SetIKHintPosition(AvatarIKHint.RightElbow, ElbowIKTarget.position);
          if (ElbowMarker.position.y > Scapula.position.y + 0f) {
            shoulder_up_target_weight = (ElbowMarker.position.y - Shoulder.position.y) * 10f;
            animator.SetFloat("shoulder_up_right", shoulder_up_weight);
          }
          else {
            shoulder_up_target_weight = 0.0f;
            animator.SetFloat("shoulder_up_right", shoulder_up_weight);
          }
          if (ElbowMarker.position.x < Shoulder.position.x) {
            shoulder_forward_target_weight = Mathf.Abs(ElbowMarker.position.x - Shoulder.position.x * 10f);
            animator.SetFloat("shoulder_forward_right", shoulder_forward_weight);
          }

          //if (ElbowMarker.position.z > Scapula.position.z) {
          //  shoulder_forward_target_weight = (ElbowMarker.position.z - (Shoulder.position.z + .15f)) * 10f;
          //  animator.SetFloat("shoulder_forward_right", shoulder_forward_weight);
          //}
          else {
            shoulder_forward_target_weight = 0.0f;
            animator.SetFloat("shoulder_forward_right", shoulder_forward_weight);
          }
        }
        else {
          animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionIKWeight);
          animator.SetIKPosition(AvatarIKGoal.RightHand, UntrackedIKPosition);
          animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationIKWeight);
          animator.SetIKRotation(AvatarIKGoal.RightHand, PalmRotationAtLateUpdate);
          animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 1);
          animator.SetIKHintPosition(AvatarIKHint.RightElbow, ElbowIKTarget.position);
        }
        if (Input.GetKey(KeyCode.X)) {
          animator.SetFloat("forearm_twist_left", 1f);
        }
        else animator.SetFloat("forearm_twist_left", 0f);
      }
    }
    private IEnumerator LerpPositionIKWeight(float destinationWeight, float duration) {
      return null;
    }


    private IEnumerator LerpToRestPosition(Vector3 droppedPosition) {
      float startTime = Time.time;
      float endTime = startTime + ArmDropDuration;

      while (Time.time <= endTime) {
        float t = (Time.time - startTime) / ArmDropDuration;
        float lerpedPositionX = Mathf.Lerp(droppedPosition.x, RestIKPosition.position.x, DropCurveX.Evaluate(t));
        float lerpedPositionY = Mathf.Lerp(droppedPosition.y, RestIKPosition.position.y, DropCurveY.Evaluate(t));
        float lerpedPositionZ = Mathf.Lerp(droppedPosition.z, RestIKPosition.position.z, DropCurveZ.Evaluate(t));
        UntrackedIKPosition = new Vector3(lerpedPositionX, lerpedPositionY, lerpedPositionZ);
        yield return null;
      }
    }


    public override void OnSetup() {
      Awake();
      MarkerPrefab = Resources.Load("AxisTripod") as GameObject;
      Debug.Log("MarkerPrefab: " + MarkerPrefab);
      Handedness = GetComponent<IHandModel>().Handedness;
      if (Handedness == Chirality.Left) {
        Scapula = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
      }
      if (Handedness == Chirality.Right) {
        Scapula = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
      }
      AssignIKMarkers();
    }
  }
}
