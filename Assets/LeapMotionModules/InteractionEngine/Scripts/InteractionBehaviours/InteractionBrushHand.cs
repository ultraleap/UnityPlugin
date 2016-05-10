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
    private const int FRAMES_DELAY = 3;

    private Rigidbody[] _capsuleBodies;
    private InteractionBrushBone[] _capsuleBones;
    private Vector3[] _lastPositions;

    private Hand _hand;
    private GameObject _handParent;
    private int _frameTimer = 0;

    private bool _hasWarned = false;

    public override ModelType HandModelType {
      get { return ModelType.Physics; }
    }

    [SerializeField]
    private Chirality handedness;
    public override Chirality Handedness {
      get { return handedness; }
    }

    [SerializeField]
    private float _perBoneMass = 0.2f; // Default to a "low" value.

    [SerializeField]
    private bool _enableDynamicMass = true;

    [SerializeField]
    private float _dynamicMassMultiplier = 0.2f; // The brushes are very powerful


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
      _frameTimer = 0;
    }


    private void ConstructHand() {
      _handParent = new GameObject(gameObject.name);
      _handParent.transform.parent = null; // Prevent hand from moving when you turn your head.

      _capsuleBodies = new Rigidbody[N_FINGERS * N_ACTIVE_BONES];
      _capsuleBones = new InteractionBrushBone[N_FINGERS * N_ACTIVE_BONES];
      _lastPositions = new Vector3[N_FINGERS * N_ACTIVE_BONES];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          GameObject capsuleGameObject = new GameObject(gameObject.name, typeof(Rigidbody), typeof(CapsuleCollider), typeof(InteractionBrushBone));
          capsuleGameObject.layer = gameObject.layer;

          Transform capsuleTransform = capsuleGameObject.transform;
          capsuleTransform.SetParent(_handParent.transform, false);
          capsuleTransform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

          CapsuleCollider capsule = capsuleGameObject.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = _material;

          Rigidbody body = capsuleGameObject.GetComponent<Rigidbody>();
          body.position = bone.Center.ToVector3();
          body.rotation = bone.Rotation.ToQuaternion();
          body.freezeRotation = true;
          body.useGravity = false;
          body.mass = _perBoneMass;
          body.collisionDetectionMode = _collisionDetection;

          _capsuleBodies[boneArrayIndex] = body;
          _capsuleBones[boneArrayIndex] = capsuleGameObject.GetComponent<InteractionBrushBone>();
          _lastPositions[boneArrayIndex] = bone.Center.ToVector3();
        }
      }
    }

    public override void UpdateHand() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
#endif

      if (_frameTimer <= FRAMES_DELAY) {
        if (_frameTimer++ < FRAMES_DELAY) {
          return;
        }
        ConstructHand();
      }

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          Rigidbody body = _capsuleBodies[boneArrayIndex];

#if UNITY_EDITOR
          // During normal operation the brushes should not be pushed away from the tracked hand.
          // In unusual situations (e.g. swatting an object really quickly) this may fire.
          if (!_hasWarned) {
            // Compare against intended target, not new tracking position.
            Vector3 error = _lastPositions[boneArrayIndex] - body.position;
            if (error.magnitude > bone.Width) {
              Debug.LogWarning("InteractionBrushHand is falling behind tracked hand: " + gameObject.name);
              _hasWarned = true;
            }
          }
          _lastPositions[boneArrayIndex] = bone.Center.ToVector3();
#endif

          Vector3 delta = bone.Center.ToVector3() - body.position;
          body.velocity = delta / Time.fixedDeltaTime;
          body.rotation = bone.Rotation.ToQuaternion();

          // Update mass
          if (_enableDynamicMass) {
            InteractionBrushBone brushBone = _capsuleBones[boneArrayIndex];
            float contactingMass = brushBone.getAverageContactingMass();
            if (contactingMass == 0) {
              body.mass = _perBoneMass;
            } else {
              body.mass = _dynamicMassMultiplier * contactingMass;
            }
          } else {
            body.mass = _perBoneMass; // !_enableDynamicMass
          }
        }
      }
    }

    public override void FinishHand() {
      if (_capsuleBodies == null)
        return; // Frame counter never expired.

      for (int i = _capsuleBodies.Length; i-- != 0;) {
        _capsuleBodies[i].transform.parent = null;
        GameObject.Destroy(_capsuleBodies[i].gameObject);
      }
      GameObject.Destroy(_handParent);
      _capsuleBodies = null;
      _capsuleBones = null;
      _lastPositions = null;

      base.FinishHand();
    }
  }
}
