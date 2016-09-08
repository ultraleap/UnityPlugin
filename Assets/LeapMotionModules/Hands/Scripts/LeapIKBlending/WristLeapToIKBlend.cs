using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
    private float elbowIKWeight;
    private float elbowIKTargetWeight;


    private Transform Hips;
    private Transform Scapula;
    private Transform Shoulder;
    private Transform Elbow;
    private Transform Neck;
    private float shoulder_up_target_weight;
    private float shoulder_up_weight;
    private float shoulder_forward_weight;
    private float shoulder_forward_target_weight;
    private float shoulder_back_weight;
    private float shoulder_back_target_weight;

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
    private Transform characterRoot;
    private float distanceShoulderToPalm;

    private Vector3 previousPalmPosition;
    private Vector3 iKVelocity;
    public Transform VelocityMarker;
    private Vector3 lastTrackedPosition;
    private Vector3 iKVelocitySnapShot;
    private Queue<Vector3> velocityList = new Queue<Vector3>();
    private Vector3 averageIKVelocity;

    protected override void Awake() {
      base.Awake();
      animator = transform.root.GetComponentInChildren<Animator>();
      characterRoot = animator.transform;
      handModel = transform.GetComponent<HandModel>();
      palm = GetComponent<HandModel>().palm;
      Neck = animator.GetBoneTransform(HumanBodyBones.Neck);
      Hips = animator.GetBoneTransform(HumanBodyBones.Hips);
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
      HandFinish();
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
      positionIKTargetWeight = 1;
      elbowIKTargetWeight = 1;
      rotationIKWeight = 0;
      shoulder_forward_target_weight = 0;
      shoulder_back_target_weight = 0;
      shoulder_up_target_weight = 0;
      shouldersLayerTargetWeight = 0f;
      spineLayerTargetWeight = 1f;

      //snapshot and constrain velocity derived
      iKVelocitySnapShot = averageIKVelocity;
      if (characterRoot.InverseTransformPoint(iKVelocitySnapShot).z < characterRoot.InverseTransformPoint(Hips.position).z + .5f) {
        iKVelocitySnapShot.z = characterRoot.InverseTransformPoint(Hips.position).z + .5f;
      }
      iKVelocitySnapShot = iKVelocitySnapShot * .5f;// scale the velocity so arm doesn't reach as far;
      VelocityMarker.position = iKVelocitySnapShot;
      StartCoroutine(MoveTowardWithVelocity(palm.position));
      lastTrackedPosition = palm.position;      
    }
    protected override void HandReset() {
      isTracking = true;
      StopAllCoroutines();
      positionIKTargetWeight = 1;
      elbowIKTargetWeight = 1;
      rotationIKWeight = 1;
      shouldersLayerTargetWeight = 1f;
      spineLayerTargetWeight = 1f;
    }
    void LateUpdate() {
      PalmPositionAtLateUpdate = palm.position;
      PalmRotationAtLateUpdate = palm.rotation;
      ElbowMarker.position = Elbow.position;
      ElbowMarker.rotation = Elbow.rotation;
      shoulder_up_weight = Mathf.Lerp(shoulder_up_weight, shoulder_up_target_weight, .4f);
      shoulder_forward_weight = Mathf.Lerp(shoulder_forward_weight, shoulder_forward_target_weight, .1f);
      shoulder_back_weight = Mathf.Lerp(shoulder_back_weight, shoulder_back_target_weight, .05f);

      positionIKWeight = Mathf.Lerp(positionIKWeight, positionIKTargetWeight, .4f);
      elbowIKWeight = Mathf.Lerp(elbowIKWeight, elbowIKTargetWeight, .4f);
      if (Handedness == Chirality.Left) {
        animator.SetLayerWeight(3, shouldersLayerTargetWeight);
      }
      if (Handedness == Chirality.Right) {
        animator.SetLayerWeight(4, shouldersLayerTargetWeight);
      }
      //animator.SetLayerWeight(2, spineLayerTargetWeight);

      //get Arm Directions and set elbow target position
      armDirection = handModel.GetArmDirection();
      Vector3 ElbowTargetPosition = characterRoot.InverseTransformPoint(palm.position + (armDirection * ElbowOffset));
      Vector3 palmInAnimatorSpace = characterRoot.InverseTransformPoint(PalmPositionAtLateUpdate);
      distanceShoulderToPalm = (palm.position - Shoulder.transform.position).magnitude;
      if (Handedness == Chirality.Left) {
        ElbowTargetPosition.x -= distanceShoulderToPalm * .6f;
      }
      if (Handedness == Chirality.Right) {
        ElbowTargetPosition.x += distanceShoulderToPalm * .6f;
      }

      if (Handedness == Chirality.Left && ElbowTargetPosition.x > -.05f) {
        //Debug.Log("Left Elbow Inside");
        ElbowTargetPosition.x = -.1f;
      }
      if (Handedness == Chirality.Right && ElbowTargetPosition.x < .05f) {
        //Debug.Log("Right Elbow Inside");
        ElbowTargetPosition.x = .1f;
      }
      ElbowIKTarget.position = characterRoot.TransformPoint(ElbowTargetPosition);

      iKVelocity = (palm.position - previousPalmPosition) / Time.deltaTime;
      if (velocityList.Count >= 3) {
        velocityList.Dequeue();
      }
      if (velocityList.Count < 3) {
        velocityList.Enqueue(iKVelocity);
      }

      averageIKVelocity = new Vector3(0, 0, 0);

      foreach (Vector3 v in velocityList) {
        averageIKVelocity += v;
      }
      averageIKVelocity = (averageIKVelocity / 3);
      //Debug.Log("iKVelocity: " + iKVelocity + " || velocityList.Count: " + velocityList.Count + " || averageIKVelocity: " + averageIKVelocity);
      previousPalmPosition = palm.position;
      if (!isTracking && Handedness == Chirality.Left) {
        Debug.DrawLine(lastTrackedPosition, iKVelocitySnapShot, Color.blue);
      }
      if (!isTracking && Handedness == Chirality.Right) {
        Debug.DrawLine(lastTrackedPosition, iKVelocitySnapShot, Color.green);
      }

    }

    public void OnAnimatorIK(int layerIndex) {
      //Debug.Log("IK");
      if (Handedness == Chirality.Left) {
        if (isTracking) {

          if (distanceShoulderToPalm < .1f) {
            Debug.Log("Hand Close to Shoulder: " + distanceShoulderToPalm);
            elbowIKTargetWeight = 0;
          }
          if (characterRoot.InverseTransformPoint(ElbowMarker.position).y > characterRoot.InverseTransformPoint(Scapula.position).y) {
            shoulder_up_target_weight = (characterRoot.InverseTransformPoint(ElbowMarker.position).y - characterRoot.InverseTransformPoint(Shoulder.position).y) * 10f;
            animator.SetFloat("shoulder_up_left", shoulder_up_weight);
          }
          else {
            shoulder_up_target_weight = 0.0f;
            animator.SetFloat("shoulder_up_left", shoulder_up_weight);
          }
          if (distanceShoulderToPalm < .2f) {
            shoulder_back_target_weight = 5 - distanceShoulderToPalm * 10;
          }
          else shoulder_back_target_weight = 0;

          if (characterRoot.InverseTransformPoint(ElbowMarker.position).x > characterRoot.InverseTransformPoint(Shoulder.position).x) {
            shoulder_forward_target_weight = Mathf.Abs(characterRoot.InverseTransformPoint(ElbowMarker.position).x - characterRoot.InverseTransformPoint(Shoulder.position).x * 20f);
            animator.SetFloat("shoulder_forward_left", shoulder_forward_weight);
          }
          //if (ElbowMarker.position.z > Scapula.position.z) {
          //  shoulder_forward_target_weight = (ElbowMarker.position.z - (Shoulder.position.z + .15f)) * 10f;
          //  animator.SetFloat("shoulder_forward_left", shoulder_forward_weight);
          //}
          else {
            shoulder_forward_target_weight = 0.0f;
          }
          shoulder_forward_target_weight += distanceShoulderToPalm * 5;
          animator.SetFloat("shoulder_forward_left", shoulder_forward_weight);
          animator.SetFloat("shoulder_back_left", shoulder_back_weight);
          animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionIKWeight);
          animator.SetIKPosition(AvatarIKGoal.LeftHand, PalmPositionAtLateUpdate);
          //animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationIKWeight * .75f);
          //animator.SetIKRotation(AvatarIKGoal.LeftHand, PalmRotationAtLateUpdate);
          animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, elbowIKWeight);
          animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ElbowIKTarget.position);
        }
        else {
          UntrackedIKHandling();
        }
      }
      if (Handedness == Chirality.Right) {
        if (isTracking) {


          if (distanceShoulderToPalm < .1f) {
            Debug.Log("Hand Close to Shoulder: " + distanceShoulderToPalm);
            elbowIKTargetWeight = 0;
          }
          if (characterRoot.InverseTransformPoint(ElbowMarker.position).y > characterRoot.InverseTransformPoint(Scapula.position).y) {
            shoulder_up_target_weight = (characterRoot.InverseTransformPoint(ElbowMarker.position).y - characterRoot.InverseTransformPoint(Shoulder.position).y) * 10f;
            animator.SetFloat("shoulder_up_right", shoulder_up_weight);
          }
          else {
            shoulder_up_target_weight = 0.0f;
            animator.SetFloat("shoulder_up_right", shoulder_up_weight);
          }
          if (distanceShoulderToPalm < .2f) {
            shoulder_back_target_weight = 5 - distanceShoulderToPalm * 10;
          }
          else shoulder_back_target_weight = 0;
          if (characterRoot.InverseTransformPoint(ElbowMarker.position).x < characterRoot.InverseTransformPoint(Shoulder.position).x) {
            shoulder_forward_target_weight = Mathf.Abs(characterRoot.InverseTransformPoint(ElbowMarker.position).x - characterRoot.InverseTransformPoint(Shoulder.position).x * 20f);
            animator.SetFloat("shoulder_forward_right", shoulder_forward_weight);
          }

          //if (ElbowMarker.position.z > Scapula.position.z) {
          //  shoulder_forward_target_weight = (ElbowMarker.position.z - (Shoulder.position.z + .15f)) * 10f;
          //  animator.SetFloat("shoulder_forward_right", shoulder_forward_weight);
          //}
          else {
            shoulder_forward_target_weight = 0.0f;
          }
          shoulder_forward_target_weight += distanceShoulderToPalm * 5;
          animator.SetFloat("shoulder_forward_right", shoulder_forward_weight);
          animator.SetFloat("shoulder_back_right", shoulder_back_weight);
          animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionIKWeight);
          animator.SetIKPosition(AvatarIKGoal.RightHand, PalmPositionAtLateUpdate);
          //animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationIKWeight  * .75f);
          //animator.SetIKRotation(AvatarIKGoal.RightHand, PalmRotationAtLateUpdate);
          animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, elbowIKWeight);
          animator.SetIKHintPosition(AvatarIKHint.RightElbow, ElbowIKTarget.position);
          //Debug.Log("distanceShoulderToPalm: " + distanceShoulderToPalm);
        }
        else {
          UntrackedIKHandling();
        }
        if (Input.GetKey(KeyCode.X)) {
          animator.SetFloat("forearm_twist_left", 1f);
        }
        else animator.SetFloat("forearm_twist_left", 0f);
      }
    }

    private void UntrackedIKHandling() {
      if (Handedness == Chirality.Left) {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionIKWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, UntrackedIKPosition);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationIKWeight);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ElbowIKTarget.position);
      }
      if (Handedness == Chirality.Right) {
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionIKWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, UntrackedIKPosition);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationIKWeight);
        animator.SetIKRotation(AvatarIKGoal.RightHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 1);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow, ElbowIKTarget.position);
      }
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

    private IEnumerator MoveTowardWithVelocity(Vector3 startPosition){
      UntrackedIKPosition = startPosition;
      float startTime = Time.time;
      float endTime = startTime + ArmDropDuration;
      float speed = averageIKVelocity.magnitude * .015f;
      float distanceToTarget = (startPosition - RestIKPosition.position).magnitude;
      if (speed < .015f ) {
        speed = .015f;
      }
      Debug.Log("speed: " + speed + " || distanceToTarget: " + distanceToTarget);
    
      while (Time.time <= endTime) {
        float t = (Time.time - startTime) / ArmDropDuration;
        UntrackedIKPosition = Vector3.MoveTowards(UntrackedIKPosition, VelocityMarker.position, speed);
        VelocityMarker.position = Vector3.Lerp(iKVelocitySnapShot, RestIKPosition.position, DropCurveX.Evaluate(t * 2));
        yield return null;
      }
    }


    public override void OnSetup() {
      Awake();
      MarkerPrefab = Resources.Load("RuntimeGizmoMarker") as GameObject;
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
