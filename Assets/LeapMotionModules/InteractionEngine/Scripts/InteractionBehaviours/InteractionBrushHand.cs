using UnityEngine;
using Leap.Unity.RuntimeGizmos;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction {
  /** 
  * A physics IHandModel implementation that works with the Interaction Engine to 
  * provide natural and subtle interaction between hands and physically
  * simulated virtual objects. 
  *
  * Use the InteractionBrushHand models when using the Interaction Engine instead of the 
  * other physics hand models.
  * @since 4.1.3
  */
  public class InteractionBrushHand : IHandModel {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;
    private const float DEAD_ZONE_FRACTION = 0.1f;
    private const float DISLOCATION_FRACTION = 5.0f;

    public InteractionBrushBone[] _brushBones;
    private Hand _hand;
    private GameObject _handParent;

    /** The model type. An InteractionBrushHand is a type of physics model. */
    public override ModelType HandModelType {
      get { return ModelType.Physics; }
    }

    /** The InteractionManager that manages this hand model for the Interaction Engine. */
    [SerializeField]
    private InteractionManager _manager;

    [SerializeField]
    private Chirality handedness;
    /** Whether this model can be used to represent a right or a left hand.*/
    public override Chirality Handedness {
      get { return handedness; }
      set { handedness = value; }
    }

    /** The physics mass value used for each bone in the hand when running the interaction simulation. */
    [SerializeField]
    private float _perBoneMass = 1.0f;

    /** The collision detection mode to use for this hand model when running physics simulations. */
    [SerializeField]
    private CollisionDetectionMode _collisionDetection = CollisionDetectionMode.ContinuousDynamic;

    /** The Unity PhysicsMaterial to use for this hand model when running physics simulation.
    * The material's Bounciness must be zero and its Bounce Combine setting must be Minimum.
    */
    [SerializeField]
    private PhysicMaterial _material = null;

    private bool handBegun = false;

    /** Gets the Leap.Hand object whose data is used to update this model. */
    public override Hand GetLeapHand() { return _hand; }
    /** Sets the Leap.Hand object to use to update this model. */
    public override void SetLeapHand(Hand hand) { _hand = hand; }

    /** Initializes this hand model. */
    public override void InitHand() {
      base.InitHand();

      if (Application.isPlaying) {
        if (_manager == null) {
          _manager = FindObjectOfType<InteractionManager>();
        }
        gameObject.layer = _manager.InteractionBrushLayer;
      }
    }

    /** Start using this hand model to represent a tracked hand. */
    public override void BeginHand() {
      base.BeginHand();

      if (handBegun) {
        for (int i = _brushBones.Length; i-- != 0;) {
          _brushBones[i].gameObject.SetActive(true);
          _brushBones[i].transform.position = _hand.PalmPosition.ToVector3();
        }
        _handParent.SetActive(true);
        return;
      }

#if UNITY_EDITOR
      if (!EditorApplication.isPlaying) {
        return;
      }

      // We also require a material for friction to be able to work.
      if (_material == null || _material.bounciness != 0.0f || _material.bounceCombine != PhysicMaterialCombine.Minimum) {
        Debug.LogError("An InteractionBrushHand must have a material with 0 bounciness and a bounceCombine of Minimum.  Name: " + gameObject.name);
      }

      checkContactState();
#endif

      _handParent = new GameObject(gameObject.name);
      _handParent.transform.parent = _manager.transform; // Prevent hand from moving when you turn your head.

#if UNITY_EDITOR
      _handParent.AddComponent<RuntimeColliderGizmos>();
#endif

      _brushBones = new InteractionBrushBone[N_FINGERS * N_ACTIVE_BONES + 1];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

          GameObject brushGameObject = new GameObject(gameObject.name, typeof(CapsuleCollider), typeof(Rigidbody), typeof(InteractionBrushBone));

          brushGameObject.transform.position = bone.Center.ToVector3();
          brushGameObject.transform.rotation = bone.Rotation.ToQuaternion();
          CapsuleCollider capsule = brushGameObject.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = _material;

          InteractionBrushBone brushBone = BeginBone(bone, brushGameObject, boneArrayIndex, capsule);

          brushBone.lastTarget = bone.Center.ToVector3();
        }
      }

      {
        // Palm is attached to the third metacarpal and derived from it.
        Bone bone = _hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = N_FINGERS * N_ACTIVE_BONES;
        GameObject brushGameObject = new GameObject(gameObject.name, typeof(BoxCollider), typeof(Rigidbody), typeof(InteractionBrushBone));

        brushGameObject.transform.position = _hand.PalmPosition.ToVector3();
        brushGameObject.transform.rotation = _hand.Rotation.ToQuaternion();
        BoxCollider box = brushGameObject.GetComponent<BoxCollider>();
        box.center = new Vector3(_hand.IsLeft ? -0.005f : 0.005f, bone.Width * -0.3f, -0.01f);
        box.size = new Vector3(bone.Length, bone.Width, bone.Length);
        box.material = _material;

        BeginBone(null, brushGameObject, boneArrayIndex, box);
      }

      //Constrain the bones to eachother to prevent separation
      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

          FixedJoint joint = _brushBones[boneArrayIndex].gameObject.AddComponent<FixedJoint>();
          joint.autoConfigureConnectedAnchor = false;
          if (jointIndex != 0) {
            Bone prevBone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            joint.connectedBody = _brushBones[boneArrayIndex - 1].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
          } else {
            joint.connectedBody = _brushBones[N_FINGERS * N_ACTIVE_BONES].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = _brushBones[N_FINGERS * N_ACTIVE_BONES].transform.InverseTransformPoint(bone.PrevJoint.ToVector3());
          }
        }
      }

      handBegun = true;
    }

    private InteractionBrushBone BeginBone(Bone bone, GameObject brushGameObject, int boneArrayIndex, Collider collider_) {
      brushGameObject.layer = gameObject.layer;
      brushGameObject.transform.localScale = Vector3.one;

      InteractionBrushBone brushBone = brushGameObject.GetComponent<InteractionBrushBone>();
      brushBone.col = collider_;
      brushBone.startTriggering();
      brushBone.manager = _manager;
      _brushBones[boneArrayIndex] = brushBone;

      Transform capsuleTransform = brushGameObject.transform;
      capsuleTransform.SetParent(_handParent.transform, false);

      Rigidbody body = brushGameObject.GetComponent<Rigidbody>();
      body.freezeRotation = true;
      brushBone.body = body;
      body.useGravity = false;
      body.collisionDetectionMode = _collisionDetection;
      if (collider_ is BoxCollider) {
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
      }

      body.mass = _perBoneMass;
      body.position = bone != null ? bone.Center.ToVector3() : _hand.PalmPosition.ToVector3();
      body.rotation = bone != null ? bone.Rotation.ToQuaternion(): _hand.Rotation.ToQuaternion();
      brushBone.lastTarget = bone != null ? bone.Center.ToVector3() : _hand.PalmPosition.ToVector3();

      return brushBone;
    }

    /** Updates this hand model. */
    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying) {
        return;
      }
#endif

      float deadzone = DEAD_ZONE_FRACTION * _hand.Fingers[1].Bone((Bone.BoneType)1).Width;

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          UpdateBone(bone, boneArrayIndex, deadzone);
        }
      }

      {
        Bone bone = _hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = N_FINGERS * N_ACTIVE_BONES;
        UpdateBone(bone, boneArrayIndex, deadzone);
      }
    }

    public void fillBones(Hand inHand) {
      if (Application.isPlaying && _brushBones.Length == N_FINGERS * N_ACTIVE_BONES + 1) {
        Vector elbowPos = inHand.Arm.ElbowPosition;
        inHand.SetTransform(_brushBones[N_FINGERS * N_ACTIVE_BONES].body.position, _brushBones[N_FINGERS * N_ACTIVE_BONES].body.rotation);

        for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
            Bone bone = inHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
            int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
            Vector displacement = _brushBones[boneArrayIndex].body.position.ToVector() - bone.Center;
            bone.Center += displacement;
            bone.PrevJoint += displacement;
            bone.NextJoint += displacement;
            bone.Rotation = _brushBones[boneArrayIndex].body.rotation.ToLeapQuaternion();
          }
        }

        inHand.Arm.PrevJoint = elbowPos; inHand.Arm.Direction = (inHand.Arm.PrevJoint - inHand.Arm.NextJoint).Normalized; inHand.Arm.Center = (inHand.Arm.PrevJoint + inHand.Arm.NextJoint) / 2f;
      }
    }

    private void UpdateBone(Bone bone, int boneArrayIndex, float deadzone) {
      InteractionBrushBone brushBone = _brushBones[boneArrayIndex];
      Rigidbody body = brushBone.body;

      // This hack works best when we set a fixed rotation for bones.  Otherwise
      // most friction is lost as the bones roll on contact.
      body.MoveRotation(bone.Rotation.ToQuaternion());

      if (brushBone.updateTriggering() == false) {
        // Calculate how far off the mark the brushes are.
        float targetingError = Vector3.Distance(brushBone.lastTarget, body.position)/bone.Width;
        float massScale = Mathf.Clamp(1.0f - (targetingError * 2.0f), 0.1f, 1.0f) * Mathf.Clamp(_hand.PalmVelocity.Magnitude * 10f, 1f, 10f);
        body.mass = _perBoneMass * massScale;

        Debug.DrawLine(brushBone.lastTarget, body.position);
        if (targetingError >= DISLOCATION_FRACTION && _hand.PalmVelocity.Magnitude < 1.5f && boneArrayIndex != N_ACTIVE_BONES*N_FINGERS) {
          brushBone.startTriggering();
        }
      }


      // Add a deadzone to avoid vibration.
      Vector3 delta = bone.Center.ToVector3() - body.position;
      float deltaLen = delta.magnitude;
      if (deltaLen <= deadzone) {
        body.velocity = Vector3.zero;
        brushBone.lastTarget = body.position;
      } else {
        delta *= (deltaLen - deadzone) / deltaLen;
        brushBone.lastTarget = body.position + delta;
        delta /= Time.fixedDeltaTime;
        body.velocity = (delta / delta.magnitude) * Mathf.Clamp(delta.magnitude, 0f, 3f);
      }
    }

    /** Cleans up this hand model when it no longer actively represents a tracked hand. */
    public override void FinishHand() {
      for (int i = _brushBones.Length; i-- != 0;) {
        _brushBones[i].gameObject.SetActive(false);
      }
      _handParent.SetActive(false);

      base.FinishHand();
    }

    private void checkContactState() {
      if (Application.isPlaying && !_manager.ContactEnabled) {
        Debug.LogError("Brush hand was created even though contact is disabled!  " +
                       "Make sure the brush group name of the Interaction Manager matches " +
                       "the actual name of the model group.");
        return;
      }
    }
  }
}
