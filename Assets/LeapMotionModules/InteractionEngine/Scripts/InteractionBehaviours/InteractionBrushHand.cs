using UnityEngine;
using UnityEngine.Assertions;
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
    private const float DISLOCATION_COUNTER = 3;

    private InteractionBrushBone[] _brushBones;
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

      _brushBones = new InteractionBrushBone[N_FINGERS * N_ACTIVE_BONES];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

          GameObject brushGameObject = new GameObject(gameObject.name, typeof(CapsuleCollider), typeof(Rigidbody), typeof(InteractionBrushBone));
          brushGameObject.layer = gameObject.layer;

          InteractionBrushBone brushBone = brushGameObject.GetComponent<InteractionBrushBone>();
          brushBone.manager = _manager;
          _brushBones[boneArrayIndex] = brushBone;

          Transform capsuleTransform = brushGameObject.transform;
          capsuleTransform.SetParent(_handParent.transform, false);
          capsuleTransform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

          CapsuleCollider capsule = brushGameObject.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = _material;
          brushBone.capsuleCollider = capsule;

          Rigidbody body = brushGameObject.GetComponent<Rigidbody>();
          brushBone.capsuleBody = body;
          body.position = bone.Center.ToVector3();
          body.rotation = bone.Rotation.ToQuaternion();
          body.freezeRotation = true;
          body.useGravity = false;

          body.mass = _perBoneMass;
          body.collisionDetectionMode = _collisionDetection;

          brushBone.lastTarget = bone.Center.ToVector3();
        }
      }
    }

    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
#endif

      float deadzone = DEAD_ZONE_FRACTION * _hand.Fingers[1].Bone((Bone.BoneType)1).Width;

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          InteractionBrushBone brushBone = _brushBones[boneArrayIndex];
          Rigidbody body = brushBone.capsuleBody;

          // This hack works best when we set a fixed rotation for bones.  Otherwise
          // most friction is lost as the bones roll on contact.
          body.MoveRotation(bone.Rotation.ToQuaternion());

          if (brushBone.updateTriggering() == false) {
            // Calculate how far off the mark the brushes are.
            float targetingError = (brushBone.lastTarget - body.position).magnitude / bone.Width;
            float massScale = Mathf.Clamp(1.0f - (targetingError * 2.0f), 0.1f, 1.0f);
            body.mass = _perBoneMass * massScale;

            if (targetingError >= DISLOCATION_FRACTION) {
              if (brushBone.dislocationCounter++ >= DISLOCATION_COUNTER) {
                brushBone.startTriggering();
                brushBone.dislocationCounter = 0;
              }
            } else {
              brushBone.dislocationCounter = 0;
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
            body.velocity = delta / Time.fixedDeltaTime;
            brushBone.lastTarget = body.position + delta;
          }
        }
      }
    }

    public override void FinishHand() {
      for (int i = _brushBones.Length; i-- != 0;) {
        GameObject.Destroy(_brushBones[i].gameObject);
      }
      GameObject.Destroy(_handParent);
      _brushBones = null;

      base.FinishHand();
    }
  }
}
