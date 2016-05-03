using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{
  /** Collision brushes */
  public class InteractionBrushHand : IHandModel
  {
    private const int N_FINGERS = 5;
    private const int N_ACTIVE_BONES = 3;

    private Rigidbody[] _capsuleBodies;
    private Hand hand_;

    public override ModelType HandModelType
    {
      get { return ModelType.Physics; }
    }

    [SerializeField]
    private Chirality handedness;
    public override Chirality Handedness
    {
      get { return handedness; }
    }

    public override Hand GetLeapHand() { return hand_; }
    public override void SetLeapHand(Hand hand) { hand_ = hand; }

    public override void InitHand()
    {
  #if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
  #endif

      _capsuleBodies = new Rigidbody[N_FINGERS * N_ACTIVE_BONES];

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++)
      {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++)
        {
          Bone bone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          GameObject capsuleGameObject = new GameObject("capsuleBody", typeof(Rigidbody), typeof(CapsuleCollider));
          capsuleGameObject.name = name;
          capsuleGameObject.layer = gameObject.layer;
          capsuleGameObject.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector;

          Transform capsuleTransform = capsuleGameObject.GetComponent<Transform>();
          capsuleTransform.parent = transform;
          capsuleTransform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

          CapsuleCollider capsule = capsuleGameObject.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width / 2f;
          capsule.height = bone.Length + bone.Width;

          Rigidbody body = capsuleGameObject.GetComponent<Rigidbody>();
          _capsuleBodies[boneArrayIndex] = body;
          body.MovePosition(bone.Center.ToVector3());
          body.MoveRotation(bone.Rotation.ToQuaternion());
          body.constraints = RigidbodyConstraints.FreezeRotation;
        }
      }
    }

    public override void UpdateHand()
    {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return;
#endif

      for (int fingerIndex = 0; fingerIndex < N_FINGERS; fingerIndex++)
      {
        for (int jointIndex = 0; jointIndex < N_ACTIVE_BONES; jointIndex++)
        {
          Bone bone = hand_.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

          int boneArrayIndex = fingerIndex * N_ACTIVE_BONES + jointIndex;
          Rigidbody body = _capsuleBodies[boneArrayIndex];
          body.velocity = (bone.Center.ToVector3() - body.position) / Time.fixedDeltaTime;
          body.rotation = bone.Rotation.ToQuaternion();
        }
      }
    }
  }
}
