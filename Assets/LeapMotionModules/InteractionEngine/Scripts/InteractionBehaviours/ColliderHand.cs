using UnityEngine;
using Leap.Unity.RuntimeGizmos;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction {

  public class ColliderHand : IHandModel {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;
    private const float DEAD_ZONE_FRACTION = 0.05f;
    private const float DISLOCATION_FRACTION = 1.5f;

    public Collider[] _colliderBones;
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
    private float _handMass = 1.0f;

    /** The collision detection mode to use for this hand model when running physics simulations. */
    [SerializeField]
    private CollisionDetectionMode _collisionDetection = CollisionDetectionMode.ContinuousDynamic;

    /** The Unity PhysicsMaterial to use for this hand model when running physics simulation.
    * The material's Bounciness must be zero and its Bounce Combine setting must be Minimum.
    */
    [SerializeField]
    private PhysicMaterial _material = null;

    Rigidbody body;
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
        for (int i = _colliderBones.Length; i-- != 0;) {
          _colliderBones[i].gameObject.SetActive(true);
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
      _handParent.transform.parent = FindObjectOfType<InteractionManager>().transform; // Prevent hand from moving when you turn your head.

#if UNITY_EDITOR
      _handParent.AddComponent<RuntimeColliderGizmos>();
#endif

      _colliderBones = new Collider[N_FINGERS * N_ACTIVE_BONES + 1];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

          GameObject brushGameObject = new GameObject(gameObject.name, typeof(CapsuleCollider));

          CapsuleCollider capsule = brushGameObject.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = _material;

          BeginBone(bone, brushGameObject, boneArrayIndex, capsule);
        }
      }

      {
        // Palm is attached to the third metacarpal and derived from it.
        Bone bone = _hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = N_FINGERS * N_ACTIVE_BONES;
        GameObject brushGameObject = new GameObject(gameObject.name, typeof(BoxCollider));

        BoxCollider box = brushGameObject.GetComponent<BoxCollider>();
        box.center = new Vector3(_hand.IsLeft ? -0.005f : 0.005f, bone.Width * -0.3f, -0.01f);
        box.size = new Vector3(bone.Length, bone.Width, bone.Length);
        box.material = _material;

        BeginBone(bone, brushGameObject, boneArrayIndex, box);
      }

      body = _handParent.AddComponent<Rigidbody>();
      body.freezeRotation = true;
      body.useGravity = false;
      body.mass = _handMass;
      body.collisionDetectionMode = _collisionDetection;

      handBegun = true;
    }

    private void BeginBone(Bone bone, GameObject brushGameObject, int boneArrayIndex, Collider collider_) {
      brushGameObject.layer = gameObject.layer;
      brushGameObject.transform.localScale = Vector3.one;
      _colliderBones[boneArrayIndex] = collider_;

      Transform capsuleTransform = brushGameObject.transform;
      capsuleTransform.SetParent(_handParent.transform, false);

      capsuleTransform.position = Quaternion.Inverse(_hand.Rotation.ToQuaternion()) * (bone.Center.ToVector3() - _hand.PalmPosition.ToVector3());
      capsuleTransform.rotation = Quaternion.Inverse(_hand.Rotation.ToQuaternion()) * bone.Rotation.ToQuaternion();
    }

    /** Updates this hand model. */
    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying) {
        return;
      }
#endif

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

          _colliderBones[boneArrayIndex].transform.localPosition = Quaternion.Inverse(_hand.Rotation.ToQuaternion()) * (bone.Center.ToVector3() - _hand.PalmPosition.ToVector3());
          _colliderBones[boneArrayIndex].transform.localRotation = Quaternion.Inverse(_hand.Rotation.ToQuaternion()) * bone.Rotation.ToQuaternion();
        }
      }

      body.MoveRotation(_hand.Rotation.ToQuaternion());

      Vector3 delta = _hand.PalmPosition.ToVector3() - body.position;
      float deltaLen = delta.magnitude;
      if (deltaLen <= 0.003f) {
        body.velocity = Vector3.zero;
      } else {
        delta *= (deltaLen - 0.003f) / deltaLen;
        body.velocity = delta / Time.fixedDeltaTime;
      }
    }

    /** Cleans up this hand model when it no longer actively represents a tracked hand. */
    public override void FinishHand() {
      for (int i = _colliderBones.Length; i-- != 0;) {
        _colliderBones[i].gameObject.SetActive(false);
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


    public void fillBones(Hand inHand) {
      if (Application.isPlaying) {
        for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
            Bone bone = inHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
            int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
            Vector displacement = _colliderBones[boneArrayIndex].transform.position.ToVector() - bone.Center;
            bone.Center += displacement;
            bone.PrevJoint += displacement;
            bone.NextJoint += displacement;
            bone.Rotation = _colliderBones[boneArrayIndex].transform.rotation.ToLeapQuaternion();
          }
        }

        //inHand.PalmPosition += _colliderBones[_colliderBones.Length - 1].body.position.ToVector() - inHand.PalmPosition;
        //inHand.Rotation = _colliderBones[_colliderBones.Length - 1].body.rotation.ToLeapQuaternion();
      }
    }
  }
}
