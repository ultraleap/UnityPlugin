/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {
  /** Collision brushes */
  public class ArticulationBrushHand : HandModel {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;

    private ArticulationBody   _palmBody; 
    private ArticulationBody[] _articulationBodies;
    private BoxCollider        _palmCollider;
    private CapsuleCollider [] _capsuleColliders;
    private int                _lastFrameTeleport = 0;
    private bool               _ghosted = false;
    private int                _layerMask;

    public override ModelType HandModelType {
      get { return ModelType.Physics; }
    }

    public Transform loPolyHandPalm;
    public SkinnedMeshRenderer loPolyHandRenderer;

    [Range(0.1f, 10f)]
    public float _strength = 1f;

    [SerializeField]
    [Tooltip("The mass of each finger bone; the palm will be 3x this.")]
    private float _perBoneMass = 3.0f;

    [SerializeField]
    [Tooltip("The physics material that the hand uses.")]
    private PhysicMaterial _material = null;

    void Start() {
      Collider[] bodies = FindObjectsOfType<Collider>();
      foreach (Collider colliderr in bodies) {
        if (colliderr.enabled && (colliderr.attachedRigidbody == null) && !colliderr.isTrigger) {
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

      // Get the layers that collide with this hand
      int myLayer = gameObject.layer;
      for (int i = 0; i < 32; i++) {
        if (!Physics.GetIgnoreLayerCollision(myLayer, i)) {
          _layerMask = _layerMask | 1 << i;
        }
      }

      if (_palmBody == null || _palmBody.gameObject == null) {
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

        _palmCollider = palmGameObject.GetComponent<BoxCollider>();
        _palmCollider.center = new Vector3(0f, 0.005f, -0.015f);
        _palmCollider.size   = new Vector3(0.06f, 0.02f, 0.07f);
        _palmCollider.material = _material;

        _palmBody = palmGameObject.GetComponent<ArticulationBody>();
        _palmBody.immovable = true;
        _palmBody.TeleportRoot(hand_.PalmPosition.ToVector3(), hand_.Rotation.ToQuaternion());
        _palmBody.mass = _perBoneMass * 3f;

        _capsuleColliders   = new CapsuleCollider [N_FINGERS * N_ACTIVE_BONES];
        _articulationBodies = new ArticulationBody[N_FINGERS * N_ACTIVE_BONES];

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
            _capsuleColliders[boneArrayIndex] = capsule;

            ArticulationBody body = capsuleGameObject.AddComponent<ArticulationBody>();
            body.anchorPosition = new Vector3(0f, 0f, 0f);
            body.anchorRotation = Quaternion.identity;
            body.mass = _perBoneMass;

            if (jointIndex == 0) {
              body.twistLock  = ArticulationDofLock .LimitedMotion;
              body.swingYLock = fingerIndex == 0 ? ArticulationDofLock.FreeMotion : ArticulationDofLock.LimitedMotion;
              body.swingZLock = ArticulationDofLock .LimitedMotion;
              body.jointType  = fingerIndex == 0 ? ArticulationJointType.SphericalJoint : ArticulationJointType.SphericalJoint;
              ArticulationDrive xDrive = new ArticulationDrive() {
                stiffness = 100f * _strength, forceLimit = 1000f * _strength, damping = 3f, lowerLimit = -15f, upperLimit = 80f
              };
              body.xDrive = xDrive;

              ArticulationDrive yDrive = new ArticulationDrive() {
                stiffness = 100f * _strength, forceLimit = 1000f * _strength, damping = 6f, lowerLimit = -15f, upperLimit = 15f
              };
              body.yDrive = yDrive;
              body.zDrive = yDrive;
            } else {
              body.jointType = ArticulationJointType.RevoluteJoint;
              body.twistLock = ArticulationDofLock  .FreeMotion;
              ArticulationDrive drive = new ArticulationDrive() {
                stiffness = 100f * _strength, forceLimit = 1000f * _strength, damping = 3f, lowerLimit = -10f, upperLimit = 89f
              };
              body.xDrive = drive;
            }

            _articulationBodies[boneArrayIndex] = body;

            lastTransform = capsuleGameObject.transform;
          }
        }
        _palmBody.solverIterations = 60;
        _palmBody.solverVelocityIterations = 20;
      } else {
        _palmBody.gameObject.SetActive(true);
        loPolyHandPalm.gameObject.SetActive(true);
        _palmCollider.enabled = false;
        foreach (CapsuleCollider collider in _capsuleColliders) collider.enabled = false;
        _palmBody.immovable = true;
        _palmBody.TeleportRoot(hand_.PalmPosition.ToVector3(), hand_.Rotation.ToQuaternion());
        _palmBody.velocity = Vector3.zero;
        _palmBody.angularVelocity = Vector3.zero;
        _lastFrameTeleport = Time.frameCount;
        for (int i = 0; i < _articulationBodies.Length; i++) {
          //_articulationBodies[i].jointVelocity = new ArticulationReducedSpace(0f, 0f, 0f);
          _articulationBodies[i].velocity = Vector3.zero;
          _articulationBodies[i].angularVelocity = Vector3.zero;
        }
        _ghosted = true;
      }
    }

    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
#endif

      // Counter Gravity; force = mass * acceleration
      _palmBody.AddForce(-Physics.gravity * _palmBody.mass);
      foreach(ArticulationBody body in _articulationBodies) {
        int dofs = body.jointVelocity.dofCount;
        float velLimit = 1.75f;
        body.maxAngularVelocity = velLimit;
        body.maxDepenetrationVelocity = 3f;

        body.AddForce(-Physics.gravity * body.mass);
      }

      // Apply tracking position velocity; force = (velocity * mass) / deltaTime
      float massOfHand = _palmBody.mass + (N_FINGERS * N_ACTIVE_BONES * _perBoneMass);
      Vector3 palmDelta = (hand_.PalmPosition.ToVector3() +
        (hand_.Rotation.ToQuaternion() * Vector3.back * 0.0225f) +
        (hand_.Rotation.ToQuaternion() * Vector3.up * 0.0115f)) - _palmBody.worldCenterOfMass;
      // Setting velocity sets it on all the joints, adding a force only adds to root joint
      //_palmBody.velocity = Vector3.zero;
      float alpha = 0.05f; // Blend between existing velocity and all new velocity
      _palmBody.velocity *= alpha;
      _palmBody.AddForce(Vector3.ClampMagnitude((((palmDelta / Time.fixedDeltaTime) / Time.fixedDeltaTime) * (_palmBody.mass + (_perBoneMass * 5))) * (1f-alpha), 1000f * _strength));

      // Apply tracking rotation velocity 
      // TODO: Compensate for phantom forces on strongly misrotated appendages
      // AddTorque and AngularVelocity both apply to ALL the joints in the chain
      Quaternion rotation = hand_.Rotation.ToQuaternion() * Quaternion.Inverse(_palmBody.transform.rotation);
      Vector3 angularVelocity = Vector3.ClampMagnitude((new Vector3(
        Mathf.DeltaAngle(0, rotation.eulerAngles.x),
        Mathf.DeltaAngle(0, rotation.eulerAngles.y),
        Mathf.DeltaAngle(0, rotation.eulerAngles.z)) / Time.fixedDeltaTime) * Mathf.Deg2Rad, 45f * _strength);
      //palmBody.angularVelocity = Vector3.zero;
      //palmBody.AddTorque(angularVelocity);
      _palmBody.angularVelocity = angularVelocity;
      _palmBody.angularDamping = 50f;

      // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
      if (Vector3.Distance(_palmBody.transform.position, hand_.PalmPosition.ToVector3()) > 1.0f) {
        _palmBody.immovable       = true;
        _palmBody.TeleportRoot(hand_.PalmPosition.ToVector3(), hand_.Rotation.ToQuaternion());
        _palmBody.velocity        = Vector3.zero;
        _palmBody.angularVelocity = Vector3.zero;
        _lastFrameTeleport       = Time.frameCount;
        _palmCollider.enabled     = false;
        foreach (CapsuleCollider collider in _capsuleColliders) collider.enabled = false;
        for (int i = 0; i < _articulationBodies.Length; i++) {
          //_articulationBodies[i].jointVelocity   = new ArticulationReducedSpace(0f, 0f, 0f);
          _articulationBodies[i].velocity        = Vector3.zero;
          _articulationBodies[i].angularVelocity = Vector3.zero;
        }
        _ghosted = true;
      }
      if (Time.frameCount - _lastFrameTeleport >= 1) {
        _palmBody.immovable = false;
        loPolyHandRenderer.enabled = true;
      }
      if (Time.frameCount - _lastFrameTeleport >= 2 && _ghosted &&
          !Physics.CheckSphere(_palmBody.worldCenterOfMass, 0.1f, _layerMask)) {
        _palmCollider.enabled = true;
        foreach (CapsuleCollider collider in _capsuleColliders) collider.enabled = true;
        _ghosted = false;
      }


      if (loPolyHandPalm != null) {
        loPolyHandPalm.position = _palmBody.transform.position - (_palmBody.transform.forward * 0.06f);
        loPolyHandPalm.rotation = _palmBody.transform.rotation * Quaternion.Euler(hand_.IsLeft ? 180f : 0f, hand_.IsLeft ? 90f : -90f, 0f);
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
            prevBone.Rotation .ToQuaternion() * ((fingerIndex == 0 && jointIndex == 0) ? -Vector3.up : Vector3.forward),
                bone.Direction.ToVector3   (),
            prevBone.Rotation .ToQuaternion() * Vector3.right);
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
      _palmBody.immovable = true;
      //palmBody.gameObject.SetActive(false); // This causes the joint references to reset!!!
      loPolyHandRenderer.enabled = false;
      _palmCollider.enabled = false;
      foreach (CapsuleCollider collider in _capsuleColliders) collider.enabled = false;
      _ghosted = true;

      base.FinishHand();
    }
  }
}
