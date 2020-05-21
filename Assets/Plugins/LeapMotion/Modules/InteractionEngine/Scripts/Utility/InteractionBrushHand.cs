/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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
  public class InteractionBrushHand : HandModel {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;
    private const float DEAD_ZONE_FRACTION = 0.05f;
    private const float DISLOCATION_FRACTION = 1.5f;

    private Rigidbody[] _brushBones;
    private Vector3[] _lastKnownPosition;
    private Hand _hand;
    private GameObject _handParent;

    public override ModelType HandModelType {
      get { return ModelType.Physics; }
    }

    public InteractionManager _manager;

    [SerializeField]
    private float _perBoneMass = 1.0f;

    [SerializeField]
    private CollisionDetectionMode _collisionDetection = CollisionDetectionMode.ContinuousDynamic;

    [SerializeField]
    private PhysicMaterial _material = null;

    private bool handBegun = false;

    public override Hand GetLeapHand() { return _hand; }
    public override void SetLeapHand(Hand hand) { _hand = hand; }

    public override void InitHand() {
      base.InitHand();

      if (Application.isPlaying) {
        gameObject.layer = _manager.contactBoneLayer;
      }
    }

    public override void BeginHand() {
      base.BeginHand();

      if (handBegun) {

        for (int i = _brushBones.Length; i-- != 0; ) {
          _brushBones[i].gameObject.SetActive(true);
          _lastKnownPosition[i] = _brushBones[i].position;
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
#endif

      _handParent = new GameObject(gameObject.name);
      _handParent.transform.parent = null; // Prevent hand from moving when you turn your head.

      _brushBones        = new Rigidbody[N_FINGERS * N_ACTIVE_BONES];
      _lastKnownPosition = new Vector3  [N_FINGERS * N_ACTIVE_BONES];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;

          GameObject brushGameObject = new GameObject(gameObject.name, typeof(CapsuleCollider), typeof(Rigidbody));//, typeof(ContactBone));
          brushGameObject.layer = gameObject.layer;

          //ContactBone brushBone = brushGameObject.GetComponent<ContactBone>();
          //brushBone. = _manager;


          Transform capsuleTransform = brushGameObject.transform;
          capsuleTransform.SetParent(_handParent.transform, false);
          capsuleTransform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

          CapsuleCollider capsule = brushGameObject.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = _material;
          //brushBone.capsuleCollider = capsule;

          Rigidbody body = brushGameObject.GetComponent<Rigidbody>();
          //brushBone.rigidbody = body;
          body.position = bone.Center.ToVector3();
          body.rotation = bone.Rotation.ToQuaternion();
          body.freezeRotation = true;
          body.useGravity = false;
          _brushBones[boneArrayIndex] = body;

          body.mass = _perBoneMass;
          body.collisionDetectionMode = _collisionDetection;

          _lastKnownPosition[boneArrayIndex] = bone.Center.ToVector3();
        }
      }
      handBegun = true;
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
          Rigidbody brushBone = _brushBones[boneArrayIndex];
          Rigidbody body = brushBone;

          // This hack works best when we set a fixed rotation for bones.  Otherwise
          // most friction is lost as the bones roll on contact.
          body.MoveRotation(bone.Rotation.ToQuaternion());

          //if (brushBone.() == false) {
            // Calculate how far off the mark the brushes are.
            float targetingError = (_lastKnownPosition[boneArrayIndex] - body.position).magnitude / bone.Width;
            float massScale = Mathf.Clamp(1.0f - (targetingError * 2.0f), 0.1f, 1.0f);
            body.mass = _perBoneMass * massScale;

            if (targetingError >= DISLOCATION_FRACTION) {
              //brushBone.startTriggering();
            }
          //}

          // Add a deadzone to avoid vibration.
          Vector3 delta = bone.Center.ToVector3() - body.position;
          float deltaLen = delta.magnitude;
          if (deltaLen <= deadzone) {
            body.velocity = Vector3.zero;
            _lastKnownPosition[boneArrayIndex] = body.position;
          } else {
            delta *= (deltaLen - deadzone) / deltaLen;
            body.velocity = delta / Time.fixedDeltaTime;
            _lastKnownPosition[boneArrayIndex] = body.position + delta;
          }
        }
      }
    }

    public override void FinishHand() {
      for (int i = _brushBones.Length; i-- != 0; ) {
        _brushBones[i].gameObject.SetActive(false);
      }
      _handParent.SetActive(false);

      base.FinishHand();
    }
  }
}
