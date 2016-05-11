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
  public class InteractionBrushHand : IHandModel {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;

    private Rigidbody[] _capsuleBodies;
    private Hand _hand;
    private GameObject _handParent;

    public override ModelType HandModelType {
      get { return ModelType.Physics; }
    }

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

    public override void BeginHand() {
      base.BeginHand();

#if UNITY_EDITOR
      if (!EditorApplication.isPlaying) {
        return;
      }

      // We also require a material for friction to be able to work.
      if (_material == null || _material.bounciness != 0.0f || _material.bounceCombine != PhysicMaterialCombine.Minimum) {
        EditorUtility.DisplayDialog("Collision Error!",
                                    "An InteractionBrushHand must have a material with 0 bounciness "
                                    + "and a bounceCombine of Minimum.  Name:" + gameObject.name,
                                    "Ok");
        Debug.Break();
      }
#endif

      _handParent = new GameObject(gameObject.name);
      _handParent.transform.parent = null; // Prevent hand from moving when you turn your head.

      _capsuleBodies = new Rigidbody[N_FINGERS * N_ACTIVE_BONES];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          GameObject capsuleGameObject = new GameObject(gameObject.name, typeof(Rigidbody), typeof(CapsuleCollider));
          capsuleGameObject.layer = gameObject.layer;
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

          Rigidbody body = capsuleGameObject.GetComponent<Rigidbody>();
          _capsuleBodies[boneArrayIndex] = body;
          body.position = bone.Center.ToVector3();
          body.rotation = bone.Rotation.ToQuaternion();
          body.freezeRotation = true;
          body.useGravity = false;

          body.mass = _perBoneMass;
          body.collisionDetectionMode = _collisionDetection;
        }
      }
    }

    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
#endif

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          Rigidbody body = _capsuleBodies[boneArrayIndex];

          Vector3 delta = bone.Center.ToVector3() - body.position;
          body.velocity = delta / Time.fixedDeltaTime;
          body.MoveRotation(bone.Rotation.ToQuaternion());
        }
      }
    }

    public override void FinishHand() {
      GameObject.Destroy(_handParent);
      _capsuleBodies = null;

      base.FinishHand();
    }
  }
}
