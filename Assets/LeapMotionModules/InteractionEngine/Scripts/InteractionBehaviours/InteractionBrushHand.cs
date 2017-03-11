using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
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
    private const float DISLOCATION_FRACTION = 3.0f;

    private InteractionBrushBone[] _brushBones;
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

    private bool _softContactEnabled = true;
    private bool _updateBrushHand = true;
    private List<SoftContact> softContacts = new List<SoftContact>(20);
    private Collider[] tempColliderArray = new Collider[1];
    private Vector3[] previousBoneCenters = new Vector3[20];
    private int softContactHysteresisTimer = 0;
    private float softContactBoneRadius = 0.02f;

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
        if (_manager != null) {
          gameObject.layer = _manager.InteractionBrushLayer;
        }else {
          gameObject.layer = 2;
        }
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
      if (_manager != null) { _handParent.transform.parent = _manager.transform; } // Prevent hand from moving when you turn your head.

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
      //enableSoftContact();
      if (_manager != null) { brushBone.manager = _manager; }
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
      body.rotation = bone != null ? bone.Rotation.ToQuaternion() : _hand.Rotation.ToQuaternion();
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

      if (_updateBrushHand) {
        float deadzone = DEAD_ZONE_FRACTION * _hand.Fingers[1].Bone((Bone.BoneType)1).Width;

        for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
            Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
            int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
            UpdateBone(bone, boneArrayIndex, deadzone);
          }
        }

        //Update Palm
        UpdateBone(_hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL), N_FINGERS * N_ACTIVE_BONES, deadzone);
        softContactHysteresisTimer++;
      }

      if (_softContactEnabled) {
        //SOFT CONTACT COLLISIONS
        softContacts.Clear();

        //Generate Contacts
        for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
            Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            int boneArrayIndex = fingerIndex * 4 + jointIndex;
            Vector3 boneCenter = bone.Center.ToVector3();

            Array.Clear(tempColliderArray, 0, tempColliderArray.Length);
            Physics.OverlapSphereNonAlloc(boneCenter, softContactBoneRadius, tempColliderArray, (_manager != null) ? 1 << _manager.InteractionLayer : ~(1 << 2));

            foreach (Collider col in tempColliderArray) {
              if (col != null && col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) {
                SoftContact contact = new SoftContact();
                contact.body = col.attachedRigidbody;

                if (col is MeshCollider) {
                  contact.normal = (contact.body.worldCenterOfMass - boneCenter).normalized;
                } else {
                  Vector3 objectLocalBoneCenter = col.transform.InverseTransformPoint(boneCenter);
                  Vector3 objectPoint = col.transform.TransformPoint(col.ClosestPointOnSurface(objectLocalBoneCenter));
                  contact.normal = (objectPoint - boneCenter).normalized * (col.IsPointInside(objectLocalBoneCenter) ? -1f : 1f);
                }
                contact.position = boneCenter + (contact.normal * softContactBoneRadius);
                contact.velocity = (boneCenter - previousBoneCenters[boneArrayIndex]) / Time.fixedDeltaTime;

                softContacts.Add(contact);
              }
            }
          }
        }

        if (softContacts.Count == 0) {
          //If there are no detected Contacts, exit soft contact mode
          disableSoftContact();
        } else {
          if (_updateBrushHand) {
            enableSoftContact();
          }

          //Otherwise, Resolve Contacts
          bool m_iterateForwards = true;
          for (int i = 0; i < 10; i++) {
            // Pick a random partition.
            int partition = UnityEngine.Random.Range(0, softContacts.Count);
            if (m_iterateForwards = !m_iterateForwards) {
              for (int it = partition; it < softContacts.Count; it++) {
                applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal);
              }
              for (int it = 0; it < partition; it++) {
                applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal);
              }
            } else {
              for (int it = partition; 0 <= it; it--) {
                applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal);
              }
              for (int it = softContacts.Count - 1; partition <= it; it--) {
                applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal);
              }
            }
            /*
            foreach (SoftContact contact in softContacts) {
              applySoftContact(contact.body, contact.position, contact.velocity, -contact.normal);
            }

            //Shuffle the list of Contacts
            int n = softContacts.Count;
            while (n > 1) {
              n--;
              int k = UnityEngine.Random.Range(0, n + 1);
              SoftContact value = softContacts[k];
              softContacts[k] = softContacts[n];
              softContacts[n] = value;
            }*/
          }
        }
      }

      //Update the last positions of the bones with this frame
      for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
          int boneArrayIndex = fingerIndex * 4 + jointIndex;
          previousBoneCenters[boneArrayIndex] = bone.Center.ToVector3();
        }
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
      if (_updateBrushHand) {
        InteractionBrushBone brushBone = _brushBones[boneArrayIndex];
        Rigidbody body = brushBone.body;

        // This hack works best when we set a fixed rotation for bones.  Otherwise
        // most friction is lost as the bones roll on contact.
        body.MoveRotation(bone.Rotation.ToQuaternion());

        // Calculate how far off the mark the brushes are.
        float targetingError = Vector3.Distance(brushBone.lastTarget, body.position) / bone.Width;
        float massScale = Mathf.Clamp(1.0f - (targetingError * 2.0f), 0.1f, 1.0f) * Mathf.Clamp(_hand.PalmVelocity.Magnitude * 10f, 1f, 10f);
        body.mass = _perBoneMass * massScale;

        //If these conditions are met, stop using brush hands to contact objects and switch to "Soft Contact"
        if (softContactHysteresisTimer > 4 && targetingError >= DISLOCATION_FRACTION && _hand.PalmVelocity.Magnitude < 1.5f && boneArrayIndex != N_ACTIVE_BONES * N_FINGERS) {
          enableSoftContact();
          return;
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
          body.velocity = (delta / delta.magnitude) * Mathf.Clamp(delta.magnitude, 0f, 7f);
        }
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
      if (Application.isPlaying && (_manager != null && !_manager.ContactEnabled)) {
        Debug.LogError("Brush hand was created even though contact is disabled!  " +
                       "Make sure the brush group name of the Interaction Manager matches " +
                       "the actual name of the model group.");
        return;
      }
    }

    struct SoftContact {
      public Rigidbody body;
      public Vector3 position;
      public Vector3 normal;
      public Vector3 velocity;
    }

    public void disableSoftContact() {
      if (!_updateBrushHand) {
        _updateBrushHand = true;
        for (int i = _brushBones.Length; i-- != 0;) {
          _brushBones[i].gameObject.SetActive(true);
          _brushBones[i].transform.position = _brushBones[i].lastTarget + (_hand.PalmPosition.ToVector3() - _brushBones[_brushBones.Length - 1].lastTarget);
          _brushBones[i].body.velocity = Vector3.zero;
        }
        _handParent.SetActive(true);
        for (int i = _brushBones.Length; i-- != 0;) {
          _brushBones[i].DisableColliderTemporarily(0.3f);
        }
        softContactHysteresisTimer = 0;
        StartCoroutine(delayedDisableSoftContact(0.3f));
      }
    }
    IEnumerator delayedDisableSoftContact(float seconds) {
      yield return new WaitForSecondsRealtime(seconds);
      if (_updateBrushHand) {
        _softContactEnabled = false;
      }
    }

    public void enableSoftContact() {
      _updateBrushHand = false;
      _softContactEnabled = true;
      for (int i = _brushBones.Length; i-- != 0;) {
        _brushBones[i].gameObject.SetActive(false);
        _brushBones[i].body.velocity = Vector3.zero;
      }
      _handParent.SetActive(false);
    }

    static void applySoftContact(Rigidbody thisObject, Vector3 handContactPoint, Vector3 handVelocityAtContactPoint, Vector3 normal) {
      Vector3 objectVelocityAtContactPoint = thisObject.GetPointVelocity(handContactPoint);
      Vector3 velocityDifference = objectVelocityAtContactPoint - handVelocityAtContactPoint;
      float relativeVelocity = Vector3.Dot(normal, velocityDifference);
      if (relativeVelocity > float.Epsilon)
        return;

      // Define a cone of friction where the direction of velocity relative to the surface
      // controls the degree to which perpendicular movement (relative to the surface) is
      // resisted.  Smoothly blending in friction prevents vibration while the impulse
      // solver is iterating.
      // Interpolation parameter 0..1: -handContactNormal.dot(vel_dir).

      Vector3 vel_dir = velocityDifference.normalized;
      float t = -relativeVelocity / velocityDifference.magnitude;
      Vector3 bentNormal = t * -vel_dir + (1.0f - t) * normal;

      if (bentNormal.magnitude > float.Epsilon) { bentNormal = bentNormal.normalized; }
      relativeVelocity = Vector3.Dot(bentNormal, velocityDifference);

      // Apply impulse along calculated normal.  Note this is slightly unusual.  Instead of
      // handling perpendicular movement as a separate constraint this calculates a "bent normal"
      // to blend it in.

      float velocityError = -relativeVelocity;
      float denom0 = computeImpulseDenominator(thisObject, handContactPoint, bentNormal);
      float jacDiagABInv = 1.0f / denom0;
      float velocityImpulse = velocityError * jacDiagABInv * 0.95f;
      Vector3 gravityFudge = -Physics.gravity * thisObject.mass * Time.fixedDeltaTime * 0.015f;

      thisObject.AddForceAtPosition(bentNormal * velocityImpulse * 0.03f + gravityFudge, handContactPoint, ForceMode.Impulse);
      //thisObject.AddForceAtPosition(((handVelocityAtContactPoint - thisObject.GetPointVelocity(handContactPoint))*50f) * relativeVelocity, handContactPoint, ForceMode.Acceleration);
    }

    static float computeImpulseDenominator(Rigidbody thisObject, Vector3 pos, Vector3 normal) {
      Vector3 r0 = pos - thisObject.worldCenterOfMass;
      Vector3 c0 = Vector3.Cross(r0, normal);
      Vector3 vec = Vector3.Cross(getInvInertiaTensorWorld(thisObject) * c0, r0);
      return (1f / thisObject.mass) + Vector3.Dot(normal, vec);
    }

    static Matrix4x4 getInvInertiaTensorWorld(Rigidbody thisObject) {
      Vector3 localInteriaTensor = thisObject.inertiaTensor;
      Quaternion bodyRotation = thisObject.rotation;
      Quaternion tensorRotation = thisObject.inertiaTensorRotation;

      Vector3 xAxis = bodyRotation * (tensorRotation * (localInteriaTensor.x * Vector3.right));
      Vector3 yAxis = bodyRotation * (tensorRotation * (localInteriaTensor.y * Vector3.up));
      Vector3 zAxis = bodyRotation * (tensorRotation * (localInteriaTensor.z * Vector3.forward));

      Debug.DrawLine(thisObject.position, thisObject.position + (xAxis), Color.red);
      Debug.DrawLine(thisObject.position, thisObject.position + (yAxis), Color.green);
      Debug.DrawLine(thisObject.position, thisObject.position + (zAxis), Color.blue);

      Matrix4x4 inertiaTensorWorld = new Matrix4x4();
      inertiaTensorWorld.SetColumn(0, xAxis);
      inertiaTensorWorld.SetColumn(1, yAxis);
      inertiaTensorWorld.SetColumn(2, zAxis);
      return inertiaTensorWorld.inverse;
    }
  }
}
