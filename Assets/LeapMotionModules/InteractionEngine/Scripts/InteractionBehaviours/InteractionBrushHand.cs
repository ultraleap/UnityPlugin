using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction {
  /** Collision brushes */
  public class InteractionBrushHand : IHandModel {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;
    private const float DEAD_ZONE_FRACTION = 0.05f;
    private const float DISLOCATION_FRACTION = 1.5f;

    private struct BrushState {
      public Rigidbody capsuleBody;
      public CapsuleCollider capsuleCollider;
      public Vector3 lastTarget;
      public bool isDislocated = false;
    }

    private BrushState[] _brushState;
    private Hand _hand;
    private GameObject _handParent;

    public override ModelType HandModelType {
      get { return ModelType.Physics; }
    }

    [SerializeField]
    private InteractionManager _manager;

    [SerializeField]
    private Chirality handedness;
    public override Chirality Handedness {
      get { return handedness; }
    }

    [SerializeField]
    private float _perBoneMass = 1.0f;

    [SerializeField]
    private CollisionDetectionMode _collisionDetection = CollisionDetectionMode.ContinuousDynamic;

    [SerializeField]
    private PhysicMaterial _material = null;

    public override Hand GetLeapHand() { return _hand; }
    public override void SetLeapHand(Hand hand) { _hand = hand; }

    public override void InitHand() {
      base.InitHand();

      if (Application.isPlaying) {
        gameObject.layer = _manager.InteractionBrushLayer;
      }
    }

    public override void BeginHand() {
      base.BeginHand();

#if UNITY_EDITOR
      if (!EditorApplication.isPlaying) {
        return;
      }

      // We also require a material for friction to be able to work.
      if (_material == null || _material.bounciness != 0.0f || _material.bounceCombine != PhysicMaterialCombine.Minimum) {
        Debug.LogError("An InteractionBrushHand must have a material with 0 bounciness and a bounceCombine of Minimum.  Name: " + gameObject.name);
      }
#endif

      _handParent = new GameObject(gameObject.name);
      _handParent.transform.parent = null; // Prevent hand from moving when you turn your head.

      _brushState = new BrushState[N_FINGERS * N_ACTIVE_BONES];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          GameObject capsuleGameObject = new GameObject(gameObject.name, typeof(Rigidbody), typeof(CapsuleCollider));
          capsuleGameObject.layer = gameObject.layer;
          capsuleGameObject.tag = "LeapMotion.InteractionBrush";
#if UNITY_EDITOR
          // This is a debug facility that warns developers of issues.
          capsuleGameObject.AddComponent<InteractionBrushBone>();
#endif

          Transform capsuleTransform = capsuleGameObject.transform;
          capsuleTransform.SetParent(_handParent.transform, false);
          capsuleTransform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

          CapsuleCollider capsule = capsuleGameObject.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = _material;
          _brushState[boneArrayIndex].capsuleCollider = capsule;

          Rigidbody body = capsuleGameObject.GetComponent<Rigidbody>();
          _brushState[boneArrayIndex].capsuleBody = body;
          body.position = bone.Center.ToVector3();
          body.rotation = bone.Rotation.ToQuaternion();
          body.freezeRotation = true;
          body.useGravity = false;

          body.mass = _perBoneMass;
          body.collisionDetectionMode = _collisionDetection;

          _brushState[boneArrayIndex].lastTarget = bone.Center.ToVector3();
        }
      }
    }


//        if (!_contactMode && results.maxHandDepth > _material.BrushDisableDistance * _manager.SimulationScale) {

    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
#endif

      float width = _hand.Fingers[1].Bone((Bone.BoneType)1).Width;
      float deadzone = DEAD_ZONE_FRACTION * width;

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          Rigidbody body = _brushState[boneArrayIndex].capsuleBody;
          body.MoveRotation(bone.Rotation.ToQuaternion());

          // Calculate how far off the mark the brushes are.
          float targetingError = (_brushState[boneArrayIndex].lastTarget - body.position).magnitude / bone.Width;
          _brushState[boneArrayIndex].lastTarget = bone.Center.ToVector3();

          // Reduce the energy applied by the brushes as they are pushed away.
          float massScale = Mathf.Clamp(1.0f - (targetingError*2.0f), 0.1f, 1.0f);
          body.mass = _perBoneMass * massScale;

          // Switch to triggering when dislocated.
          bool isDislocated = targetingError >= DISLOCATION_FRACTION;
          if(isDislocated != _brushState[boneArrayIndex].isDislocated) {
            _brushState[boneArrayIndex].isDislocated = isDislocated;
            _brushState[boneArrayIndex].capsuleCollider.isTrigger = isDislocated;
          }

          // Add a deadzone to avoid vibration.
          Vector3 delta = bone.Center.ToVector3() - body.position;
          float deltaLen = delta.magnitude;
          if (deltaLen <= deadzone) {
            body.velocity = Vector3.zero;
            continue;
          }
          delta *= (deltaLen - deadzone) / deltaLen;

          body.velocity = delta / Time.fixedDeltaTime;
        }
      }
    }

    public override void FinishHand() {
      GameObject.Destroy(_handParent);
      _brushState = null;

      base.FinishHand();
    }

#if UNITY_EDITOR
    public override void OnDrawGizmos(){
      Matrix4x4 gizmosMatrix = Gizmos.matrix;

      float radius = _hand.Fingers[1].Bone((Bone.BoneType)1).Width;

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          Rigidbody body = _brushState[boneArrayIndex].capsuleBody;

          Gizmos.matrix = body.transform.localToWorldMatrix;
          Gizmos.color = _brushState[boneArrayIndex].isDislocated ? Color.red : Color.green;
          Gizmos.DrawWireSphere(body.centerOfMass, radius);
        }
      }

      Gizmos.matrix = gizmosMatrix;
    }
#endif
  }
}
