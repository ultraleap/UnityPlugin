using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {
  /** Collision brushes */
  public class ArticulationBrushHand : HandModel {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;

    private ArticulationBody palmBody;
    private ArticulationBody[] _articulationBodies;
    private CapsuleCollider[] _capsuleBodies;
    private Vector3 _palmBodyLastPos;
    private Vector3[] _bodyLastPositions;
    private int _lastFrameTeleport = 0;

    public override ModelType HandModelType {
      get { return ModelType.Physics; }
    }

    public Transform loPolyHandPalm;

    [SerializeField]
    [Tooltip("The mass of each finger bone; the palm will be 3x this.")]
    private float _perBoneMass = 3.0f;

    [SerializeField]
    [Tooltip("The physics material that the hand uses.")]
    private PhysicMaterial _material = null;

    void Start() {
      Collider[] bodies = FindObjectsOfType<Collider>();
      foreach (Collider colliderr in bodies) {
        if ((colliderr.attachedRigidbody == null) && !colliderr.isTrigger) {
          colliderr.gameObject.AddComponent<Rigidbody>().isKinematic = true;
        }
      }
    }

    public override Hand GetLeapHand() { return hand_; }
    public override void SetLeapHand(Hand hand) { hand_ = hand; }

    public override void BeginHand() {
      base.BeginHand();

#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;

      // We also require a material for friction to be able to work.
      if (_material == null || _material.bounciness != 0.0f || _material.bounceCombine != PhysicMaterialCombine.Minimum) {
        UnityEditor.EditorUtility.DisplayDialog("Collision Error!",
                                                "An InteractionBrushHand must have a material with 0 bounciness "
                                                + "and a bounceCombine of Minimum.  Name:" + gameObject.name,
                                                "Ok");
        Debug.Break();
      }
#endif


      if (palmBody == null || palmBody.gameObject == null) {
        GameObject palmGameObject = new GameObject(gameObject.name + " Palm", typeof(ArticulationBody), typeof(BoxCollider));
        palmGameObject.layer = gameObject.layer;

        Transform palmTransform = palmGameObject.GetComponent<Transform>();
        //palmTransform.parent = transform;
        palmTransform.position = hand_.PalmPosition.ToVector3();
        palmTransform.rotation = hand_.Rotation.ToQuaternion();
        if (palmTransform.parent != null) {
          palmTransform.localScale = new Vector3(
            1f / palmTransform.parent.lossyScale.x, 
            1f / palmTransform.parent.lossyScale.y, 
            1f / palmTransform.parent.lossyScale.z);
        }

        BoxCollider box = palmGameObject.GetComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.005f, -0.015f);
        box.size   = new Vector3(0.06f, 0.02f, 0.07f);
        box.material = _material;

        palmBody = palmGameObject.GetComponent<ArticulationBody>();
        palmBody.immovable = true;
        palmBody.TeleportRoot(hand_.PalmPosition.ToVector3(), hand_.Rotation.ToQuaternion());
        palmBody.mass = _perBoneMass * 3f;

        _capsuleBodies      = new CapsuleCollider [N_FINGERS * N_ACTIVE_BONES];
        _articulationBodies = new ArticulationBody[N_FINGERS * N_ACTIVE_BONES];
        _bodyLastPositions  = new Vector3         [N_FINGERS * N_ACTIVE_BONES];

        for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
          Transform lastTransform = palmTransform;
          Bone knuckleBone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(0));
          for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
            Bone prevBone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            Bone bone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

            int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

            GameObject capsuleGameObject;
            capsuleGameObject = new GameObject(gameObject.name + " Finger " + boneArrayIndex, typeof(CapsuleCollider));
            capsuleGameObject.layer = gameObject.layer;

            capsuleGameObject.transform.parent = lastTransform;
            if (jointIndex == 0) {
              capsuleGameObject.transform.position = fingerIndex == 0 ? knuckleBone.PrevJoint.ToVector3() : knuckleBone.NextJoint.ToVector3();
            } else {
              capsuleGameObject.transform.localPosition = Vector3.forward * prevBone.Length;
            }
            capsuleGameObject.transform.rotation = knuckleBone.Rotation.ToQuaternion();

            if (capsuleGameObject.transform.parent != null) {
               capsuleGameObject.transform.localScale = new Vector3(
                1f / capsuleGameObject.transform.parent.lossyScale.x,
                1f / capsuleGameObject.transform.parent.lossyScale.y,
                1f / capsuleGameObject.transform.parent.lossyScale.z);
            }

            CapsuleCollider capsule = capsuleGameObject.GetComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.radius = bone.Width * 0.5f;
            capsule.height = bone.Length + bone.Width;
            capsule.material = _material;
            capsule.center = new Vector3(0f, 0f, bone.Length / 2f);
            _capsuleBodies[boneArrayIndex] = capsule;

            ArticulationBody body = capsuleGameObject.AddComponent<ArticulationBody>();
            body.anchorPosition = new Vector3(0f, 0f, 0f);
            body.anchorRotation = Quaternion.identity;
            body.mass = _perBoneMass;

            if (jointIndex == 0) {
              body.twistLock  = ArticulationDofLock  .FreeMotion;
              body.swingYLock = fingerIndex == 0 ? ArticulationDofLock.FreeMotion : ArticulationDofLock.LockedMotion;
              body.swingZLock = ArticulationDofLock  .FreeMotion;
              body.jointType  = fingerIndex == 0 ? ArticulationJointType.SphericalJoint : ArticulationJointType.RevoluteJoint; //ArticulationJointType.SphericalJoint;
              ArticulationDrive xDrive = new ArticulationDrive() {
                stiffness = 10f, forceLimit = 1000f, damping = 0.25f, lowerLimit = -5f, upperLimit = 80f
              };
              body.xDrive = xDrive;

              ArticulationDrive yDrive = new ArticulationDrive() {
                stiffness = 10f, forceLimit = 1000f, damping = 0.5f, lowerLimit = -20f, upperLimit = 20f
              };
              body.yDrive = yDrive;
              body.zDrive = yDrive;
            } else {
              body.jointType = ArticulationJointType.RevoluteJoint;
              body.twistLock = ArticulationDofLock  .FreeMotion;
              ArticulationDrive drive = new ArticulationDrive() {
                stiffness = 10f, forceLimit = 1000f, damping = 0.25f, lowerLimit = -10f, upperLimit = 89f
              };
              body.xDrive = drive;
            }

            _articulationBodies[boneArrayIndex] = body;

            lastTransform = capsuleGameObject.transform;
          }
        }
      } else {
        palmBody.gameObject.SetActive(true);
        palmBody.immovable = true;
        palmBody.TeleportRoot(hand_.PalmPosition.ToVector3(), hand_.Rotation.ToQuaternion());
        _lastFrameTeleport = Time.frameCount;
      }
    }

    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
#endif

      Collider palmCollider = palmBody.GetComponent<BoxCollider>();

      // Counter Gravity; force = mass * acceleration
      palmBody.AddForce(-Physics.gravity * palmBody.mass);
      foreach(ArticulationBody body in _articulationBodies) { body.AddForce(-Physics.gravity * body.mass); }

      // Counter Velocity; force = (velocity * mass) / deltaTime
      palmBody.AddForce(Vector3.ClampMagnitude(1.0f * -(((palmBody.worldCenterOfMass - _palmBodyLastPos) / Time.fixedDeltaTime) * palmBody.mass) / Time.fixedDeltaTime, 1500f));
      for (int i = 0; i < _articulationBodies.Length; i++) {
        Vector3 velocity = (_articulationBodies[i].worldCenterOfMass - _bodyLastPositions[i]) / Time.fixedDeltaTime;
        _articulationBodies[i].AddForce(1.0f * Vector3.ClampMagnitude(-(velocity * _articulationBodies[i].mass) / Time.fixedDeltaTime, 5f)); 
      }

      // Record positions to calculate velocity
      _palmBodyLastPos = palmBody.worldCenterOfMass;
      for(int i = 0; i < _articulationBodies.Length; i++) { _bodyLastPositions[i] = _articulationBodies[i].worldCenterOfMass; }

      // Apply tracking position velocity; force = (velocity * mass) / deltaTime
      float massOfHand = palmBody.mass + (N_FINGERS * N_ACTIVE_BONES * _perBoneMass);
      Vector3 palmDelta = (hand_.PalmPosition.ToVector3() + 
        (hand_.Rotation.ToQuaternion() * Vector3.back * 0.0225f) +
        (hand_.Rotation.ToQuaternion() * Vector3.up * 0.0115f)) - palmBody.worldCenterOfMass;
      palmBody.AddForce(Vector3.ClampMagnitude(((palmDelta / Time.fixedDeltaTime) * palmBody.mass) / Time.fixedDeltaTime, 2000f));

      // Apply tracking rotation velocity TODO: Make this correct...
      Quaternion rotation = hand_.Rotation.ToQuaternion() * Quaternion.Inverse(palmBody.transform.rotation);
      Vector3 angularVelocity = Vector3.ClampMagnitude((new Vector3(
        Mathf.DeltaAngle(0, rotation.eulerAngles.x),
        Mathf.DeltaAngle(0, rotation.eulerAngles.y),
        Mathf.DeltaAngle(0, rotation.eulerAngles.z)) / Time.fixedDeltaTime) / 50f, 150f);
      palmBody.AddTorque(angularVelocity);

      // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
      if (Vector3.Distance(palmBody.transform.position, hand_.PalmPosition.ToVector3()) > 1.0f) {
        palmBody.immovable = true;
        palmBody.TeleportRoot(hand_.PalmPosition.ToVector3(), hand_.Rotation.ToQuaternion());
        _lastFrameTeleport = Time.frameCount;
      }
      if (Time.frameCount - _lastFrameTeleport >= 20) palmBody.immovable = false;


      if (loPolyHandPalm != null) {
        loPolyHandPalm.position = palmBody.transform.position - (palmBody.transform.forward * 0.06f);
        loPolyHandPalm.rotation = palmBody.transform.rotation * Quaternion.Euler(hand_.IsLeft ? 180f : 0f, hand_.IsLeft ? 90f : -90f, 0f);
      }

      Transform curJoint = null;
      // Iterate through the bones in the hand, applying drive forces
      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        if (loPolyHandPalm != null) {
          curJoint = fingerIndex == 0 ? loPolyHandPalm.GetChild(fingerIndex) : loPolyHandPalm.GetChild(fingerIndex).GetChild(0);
        }
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone prevBone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
          Bone bone     = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

          int boneArrayIndex    = fingerIndex * N_ACTIVE_BONES + jointIndex;
          ArticulationBody body = _articulationBodies[boneArrayIndex];

          float xTargetAngle = Vector3.SignedAngle(
            prevBone.Rotation.ToQuaternion() * ((fingerIndex == 0 && jointIndex == 0) ? -Vector3.up : Vector3.forward),
            bone.Direction.ToVector3(), prevBone.Rotation.ToQuaternion() * Vector3.right);
          xTargetAngle = (fingerIndex == 0 && jointIndex == 0) ? xTargetAngle + 90f : xTargetAngle;

          body.xDrive = new ArticulationDrive() {
            stiffness  = body.xDrive.stiffness, 
            forceLimit = body.xDrive.forceLimit, 
            damping    = body.xDrive.damping,
            lowerLimit = body.xDrive.lowerLimit,
            upperLimit = body.xDrive.upperLimit,
            target     = xTargetAngle
          };

          if (jointIndex == 0) {
            float yTargetAngle = Vector3.SignedAngle(
              prevBone.Rotation.ToQuaternion() * Vector3.right,
              bone    .Rotation.ToQuaternion() * Vector3.right,
              prevBone.Rotation.ToQuaternion() * Vector3.up);

            body.yDrive = new ArticulationDrive() {
              stiffness  = body.yDrive.stiffness, 
              forceLimit = body.yDrive.forceLimit, 
              damping    = body.yDrive.damping,
              upperLimit = body.yDrive.upperLimit,
              lowerLimit = body.yDrive.lowerLimit,
              target     = yTargetAngle
            };
          }

          CapsuleCollider CapCollider = _capsuleBodies[boneArrayIndex];
          if (CapCollider) {
            Vector3 a = bone.PrevJoint.ToVector3(), 
                    b = bone.NextJoint.ToVector3();
          }

          if (loPolyHandPalm != null) {
            curJoint.transform.position = body.transform.position;
            curJoint.transform.rotation = body.transform.rotation * Quaternion.Euler(hand_.IsLeft ? 180f : 0f, hand_.IsLeft ? 90f : -90f, 0f);
            if (curJoint.childCount > 0) {
              curJoint = curJoint.GetChild(0);
            }
          }
        }
      }
    }

    public override void FinishHand() {
      palmBody.immovable = true;
      //palmBody.gameObject.SetActive(false); // This causes the joint references to reset!!!

      base.FinishHand();
    }
  }
}
