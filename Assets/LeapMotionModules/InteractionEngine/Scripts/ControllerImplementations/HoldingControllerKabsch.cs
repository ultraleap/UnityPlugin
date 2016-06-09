using UnityEngine;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class HoldingControllerKabsch : IHoldingController {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    protected Dictionary<int, HandPointCollection> _handIdToPoints;
    protected Rigidbody _rigidbody;
    protected LEAP_IE_KABSCH _kabsch;

    protected override void Init(InteractionBehaviour obj, InteractionManager manager) {
      base.Init(obj, manager);

      _rigidbody = obj.Rigidbody;
      KabschC.Construct(ref _kabsch);
    }

    public override void AddHand(Hand hand) {
      var newCollection = HandPointCollection.Create(_rigidbody);
      _handIdToPoints[hand.Id] = newCollection;

      newCollection.UpdateTransform();

      for (int f = 0; f < NUM_FINGERS; f++) {
        Finger finger = hand.Fingers[f];
        Finger.FingerType fingerType = finger.Type;

        for (int j = 0; j < NUM_BONES; j++) {
          Bone.BoneType boneType = (Bone.BoneType)j;
          Bone bone = finger.Bone(boneType);

          Vector3 bonePos = bone.NextJoint.ToVector3();

          //Global position of the point is just the position of the joint itself
          newCollection.SetGlobalPosition(bonePos, fingerType, boneType);
        }
      }
    }

    public override void RemoveHand(Hand hand) {
      var collection = _handIdToPoints[hand.Id];
      _handIdToPoints.Remove(hand.Id);

      //Return the collection to the pool so it can be re-used
      HandPointCollection.Return(collection);
    }

    public override void GetHeldTransform(ReadonlyList<Hand> hands, out Vector3 newPosition, out Quaternion newRotation) {
      KabschC.Reset(ref _kabsch);

      for (int h = 0; h < hands.Count; h++) {
        Hand hand = hands[h];

        var collection = _handIdToPoints[hand.Id];
        collection.UpdateTransform();

        for (int f = 0; f < NUM_FINGERS; f++) {
          Finger finger = hand.Fingers[f];
          Finger.FingerType fingerType = finger.Type;

          for (int j = 0; j < NUM_BONES; j++) {
            Bone.BoneType boneType = (Bone.BoneType)j;
            Bone bone = finger.Bone(boneType);

            Vector3 objectPos = collection.GetGlobalPosition(fingerType, boneType);
            Vector3 bonePos = bone.NextJoint.ToVector3();

            //Do the solve such that the objects positions are matched to the new bone positions
            LEAP_VECTOR point1 = (objectPos - _rigidbody.position).ToCVector();
            LEAP_VECTOR point2 = (bonePos - _rigidbody.position).ToCVector();

            KabschC.AddPoint(ref _kabsch, ref point1, ref point2, 1.0f);
          }
        }
      }

      LEAP_VECTOR leapTranslation;
      LEAP_QUATERNION leapRotation;
      KabschC.GetTranslation(ref _kabsch, out leapTranslation);
      KabschC.GetRotation(ref _kabsch, out leapRotation);

      Vector3 solvedTranslation = leapTranslation.ToVector3();
      Quaternion solvedRotation = leapRotation.ToQuaternion();

      //Calculate new transform using delta
      newPosition = _rigidbody.position + solvedTranslation;
      newRotation = solvedRotation * _rigidbody.rotation; ;
    }

    protected void performSolve() {
      KabschC.Solve(ref _kabsch);
    }

    protected class HandPointCollection {
      //Without a pool, you might end up with 2 instances per object
      //With a pool, likely there will only ever be 2 instances!
      private static Stack<HandPointCollection> _handPointCollectionPool = new Stack<HandPointCollection>();

      private Rigidbody _rigidbody;
      private Vector3[] _localPositions;

      private Matrix4x4 _transformMatrix;

      private bool _hasInverse = false;
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

      private HandPointCollection() {
        _localPositions = new Vector3[NUM_FINGERS * NUM_BONES];
      }

      private void init(Rigidbody rigidbody) {
        _rigidbody = rigidbody;
      }

      private void reset() {
        _rigidbody = null;
        _hasInverse = false;
      }

      public void UpdateTransform() {
        Vector3 interactionPosition = _rigidbody.position;
        Quaternion interactionRotation = _rigidbody.rotation;

        _hasInverse = false;
        _transformMatrix = Matrix4x4.TRS(interactionPosition, interactionRotation, Vector3.one);
      }

      public void SetGlobalPosition(Vector3 globalPosition, Finger.FingerType fingerType, Bone.BoneType boneType) {
        if (!_hasInverse) {
          _inverseTransformMatrix = _transformMatrix.inverse;
          _hasInverse = true;
        }

        _localPositions[getIndex(fingerType, boneType)] = _inverseTransformMatrix.MultiplyPoint3x4(globalPosition);
      }

      public Vector3 GetGlobalPosition(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return _transformMatrix.MultiplyPoint3x4(_localPositions[getIndex(fingerType, boneType)]);
      }

      private int getIndex(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return (int)fingerType * 4 + (int)boneType;
      }
    }
  }
}
