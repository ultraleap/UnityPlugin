using UnityEngine;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class KabschInteractionBehaviour : InteractionBehaviour {
    public const int NUM_FINGERS = 5;
    public const int NUM_JOINTS = 4;

    protected Dictionary<int, HandPointCollection> _handIdToPoints;

    protected Rigidbody _rigidbody;
    protected LEAP_IE_KABSCH _kabsch;

    #region INTERACTION CALLBACKS
    public override void OnPush(Vector3 linearVelocity, Vector3 angularVelocity) {
      base.OnPush(linearVelocity, angularVelocity);

      if (_rigidbody != null) {
        _rigidbody.velocity = linearVelocity;
        _rigidbody.angularVelocity = angularVelocity;
      }
    }

    public override void OnHandGrasp(Hand hand) {
      base.OnHandGrasp(hand);

      var newCollection = HandPointCollection.Create(_rigidbody);
      _handIdToPoints[hand.Id] = newCollection;
      newCollection.UpdateTransform();

      for (int f = 0; f < NUM_FINGERS; f++) {
        Finger finger = hand.Fingers[f];
        Finger.FingerType fingerType = finger.Type;

        for (int j = 0; j < NUM_JOINTS; j++) {
          Bone.BoneType boneType = (Bone.BoneType)j;
          Bone bone = finger.Bone(boneType);

          Vector3 jointPos = bone.NextJoint.ToVector3();
          Vector3 closestOnSurface = jointPos; //TODO: how to get closest on surface?

          newCollection.SetGlobalPosition(closestOnSurface, fingerType, boneType);
        }
      }
    }

    public override void OnHandsHold(List<Hand> hands) {
      base.OnHandsHold(hands);

      KabschC.Reset(ref _kabsch);
      for (int h = 0; h < hands.Count; h++) {
        Hand hand = hands[h];

        var collection = _handIdToPoints[hand.Id];
        collection.UpdateTransform();

        for (int f = 0; f < NUM_FINGERS; f++) {
          Finger finger = hand.Fingers[f];
          Finger.FingerType fingerType = finger.Type;

          for (int j = 0; j < NUM_JOINTS; j++) {
            Bone.BoneType boneType = (Bone.BoneType)j;
            Bone bone = finger.Bone(boneType);

            Vector3 jointPos = bone.NextJoint.ToVector3();
            Vector3 coresponding = collection.GetGlobalPosition(fingerType, boneType);

            LEAP_VECTOR point1 = new LEAP_VECTOR(jointPos);
            LEAP_VECTOR point2 = new LEAP_VECTOR(coresponding);

            KabschC.AddPoint(ref _kabsch, ref point1, ref point2, 1.0f);
          }
        }
      }

      KabschC.Solve(ref _kabsch);


    }

    public override void OnHandRelease(Hand hand) {
      base.OnHandRelease(hand);

      var collection = _handIdToPoints[hand.Id];
      _handIdToPoints.Remove(hand.Id);
      HandPointCollection.Return(collection);
    }

    public override void OnHandLostTracking(Hand oldHand) {
      base.OnHandLostTracking(oldHand);
    }

    public override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);
    }

    public override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();
    }

    #endregion

    #region UNITY CALLBACKS
    protected virtual void Awake() {
      _handIdToPoints = new Dictionary<int, HandPointCollection>();
      KabschC.Construct(ref _kabsch);
    }

    protected virtual void OnEnable() {
      EnableInteraction();
    }

    protected virtual void Start() {
      _rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void OnDisable() {
      DisableInteraction();
    }

    protected virtual void OnDestroy() {
      KabschC.Destruct(ref _kabsch);
    }
    #endregion

    #region CLASSES
    protected class HandPointCollection {
      private static Stack<HandPointCollection> _handPointCollectionPool = new Stack<HandPointCollection>();

      protected Rigidbody _rigidbody;
      protected Vector3[] _localPositions;

      private Matrix4x4 _transformMatrix;
      private Matrix4x4 _inverseTransformMatrix;

      public static HandPointCollection Create(Rigidbody rigidbody) {
        HandPointCollection collection;
        if (_handPointCollectionPool.Count != 0) {
          collection = _handPointCollectionPool.Pop();
        } else {
          collection = new HandPointCollection();
        }

        collection.init(rigidbody);
        return collection;
      }

      public static void Return(HandPointCollection handPointCollection) {
        handPointCollection.reset();
        _handPointCollectionPool.Push(handPointCollection);
      }

      private HandPointCollection() { }

      private void init(Rigidbody rigidbody) {
        _localPositions = new Vector3[NUM_FINGERS * NUM_JOINTS];
      }

      private void reset() {
        _rigidbody = null;
      }

      public void UpdateTransform() {
        _transformMatrix = Matrix4x4.TRS(_rigidbody.position, _rigidbody.rotation, Vector3.one);
        _inverseTransformMatrix = Matrix4x4.Inverse(_transformMatrix);
      }

      public void SetGlobalPosition(Vector3 globalPosition, Finger.FingerType fingerType, Bone.BoneType boneType) {
        _localPositions[getIndex(fingerType, boneType)] = _inverseTransformMatrix.MultiplyPoint3x4(globalPosition);
      }

      public Vector3 GetGlobalPosition(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return _transformMatrix.MultiplyPoint3x4(_localPositions[getIndex(fingerType, boneType)]);
      }

      private int getIndex(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return (int)fingerType * 5 + (int)boneType;
      }
    }
    #endregion


  }

}
