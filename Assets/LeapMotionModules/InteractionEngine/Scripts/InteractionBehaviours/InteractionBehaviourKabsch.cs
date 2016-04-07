using UnityEngine;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class InteractionBehaviourKabsch : InteractionBehaviourBase {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    [Tooltip("Renderers to turn off when the object is grasped by untracked hands.")]
    [SerializeField]
    protected Renderer[] _toggledRenderers;

    protected Dictionary<int, HandPointCollection> _handIdToPoints;

    private bool _shouldNextSolveBeTeleport = false;

    protected LEAP_IE_KABSCH _kabsch;

    #region INTERACTION CALLBACKS

    public override INTERACTION_TRANSFORM InteractionTransform {
      get {
        INTERACTION_TRANSFORM interactionTransform = new INTERACTION_TRANSFORM();
        interactionTransform.position = _rigidbody.position.ToCVector();
        interactionTransform.rotation = _rigidbody.rotation.ToCQuaternion();
        return interactionTransform;
      }
    }

    public override void OnRegister() {
      base.OnRegister();
      KabschC.Construct(ref _kabsch);
    }

    protected override void OnDisable() {
      base.OnDisable();
      KabschC.Destruct(ref _kabsch);
    }

    public override void OnHandGrasp(Hand hand) {
      base.OnHandGrasp(hand);

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
          Vector3 closestOnSurface = bonePos; //TODO: how to get closest on surface?

          newCollection.SetGlobalPosition(closestOnSurface, fingerType, boneType);
        }
      }
    }

    public override void OnHandsHold(List<Hand> hands) {
      base.OnHandsHold(hands);

      //Get old transform
      Vector3 oldPosition = _rigidbody.position;
      Quaternion oldRotation = _rigidbody.rotation;

      //Get solved transform deltas
      Vector3 solvedTranslation;
      Quaternion solvedRotation;
      getSolvedTransform(hands, oldPosition, out solvedTranslation, out solvedRotation);

      //Calculate new transform using delta
      Vector3 newPosition = oldPosition + solvedTranslation;
      Quaternion newRotation = solvedRotation * oldRotation;

      //Apply new transform to object
      if (_shouldNextSolveBeTeleport) {
        _rigidbody.position = newPosition;
        _rigidbody.rotation = newRotation;
        _shouldNextSolveBeTeleport = false;
      } else {
        _rigidbody.MovePosition(newPosition);
        _rigidbody.MoveRotation(newRotation);
      }
    }

    public override void OnHandRelease(Hand hand) {
      base.OnHandRelease(hand);

      removeHandPointCollection(hand.Id);
    }

    public override void OnHandLostTracking(Hand oldHand) {
      base.OnHandLostTracking(oldHand);

      updateRendererStatus();
    }

    public override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);

      updateRendererStatus();

      //Associate the collection with the new id
      var collection = _handIdToPoints[oldId];
      _handIdToPoints.Remove(oldId);
      _handIdToPoints[newHand.Id] = collection;

      _shouldNextSolveBeTeleport = true;
    }

    public override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);

      updateRendererStatus();
      removeHandPointCollection(oldHand.Id);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();

      if (_rigidbody != null) {
        _rigidbody.isKinematic = true;
      }
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();

      if (_rigidbody != null) {
        _rigidbody.isKinematic = false;
      }
    }
    #endregion

    #region UNITY CALLBACKS
    protected override void Reset() {
      base.Reset();
      _toggledRenderers = GetComponentsInChildren<Renderer>();
    }

    protected virtual void Awake() {
      _handIdToPoints = new Dictionary<int, HandPointCollection>();
    }
    #endregion

    #region INTERNAL
    protected virtual void updateRendererStatus() {
      //Renderers are visible if there are no grasping hands
      //or if there is at least one tracked grasping hand
      int trackedGraspingHandCount = GraspingHandCount - UntrackedHandCount;
      bool shouldBeVisible = GraspingHandCount == 0 || trackedGraspingHandCount > 0;

      for (int i = 0; i < _toggledRenderers.Length; i++) {
        Renderer renderer = _toggledRenderers[i];
        if (renderer != null) {
          renderer.enabled = shouldBeVisible;
        }
      }
    }

    protected void removeHandPointCollection(int handId) {
      var collection = _handIdToPoints[handId];
      _handIdToPoints.Remove(handId);
      HandPointCollection.Return(collection);
    }

    protected void getSolvedTransform(List<Hand> hands, Vector3 oldPosition, out Vector3 translation, out Quaternion rotation) {
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
            LEAP_VECTOR point1 = (objectPos - oldPosition).ToCVector();
            LEAP_VECTOR point2 = (bonePos - oldPosition).ToCVector();

            KabschC.AddPoint(ref _kabsch, ref point1, ref point2, 1.0f);
          }
        }
      }

      KabschC.Solve(ref _kabsch);

      LEAP_VECTOR leapTranslation;
      LEAP_QUATERNION leapRotation;
      KabschC.GetTranslation(ref _kabsch, out leapTranslation);
      KabschC.GetRotation(ref _kabsch, out leapRotation);

      translation = leapTranslation.ToVector3();
      rotation = leapRotation.ToQuaternion();
    }

    protected class HandPointCollection {
      //Without a pool, you might end up with 2 instances per object
      //With a pool, likely there will only ever be 2 instances!
      private static Stack<HandPointCollection> _handPointCollectionPool = new Stack<HandPointCollection>();

      private Rigidbody _rigidbody;
      private Vector3[] _localPositions;

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

      private HandPointCollection() {
        _localPositions = new Vector3[NUM_FINGERS * NUM_BONES];
      }

      private void init(Rigidbody rigidbody) {
        _rigidbody = rigidbody;
      }

      private void reset() {
        _rigidbody = null;
      }

      public void UpdateTransform() {
        Vector3 interactionPosition = _rigidbody.position;
        Quaternion interactionRotation = _rigidbody.rotation;

        _transformMatrix = Matrix4x4.TRS(interactionPosition, interactionRotation, Vector3.one);
        _inverseTransformMatrix = Matrix4x4.Inverse(_transformMatrix);
      }

      public void SetGlobalPosition(Vector3 globalPosition, Finger.FingerType fingerType, Bone.BoneType boneType) {
        _localPositions[getIndex(fingerType, boneType)] = _inverseTransformMatrix.MultiplyPoint3x4(globalPosition);
      }

      public Vector3 GetGlobalPosition(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return _transformMatrix.MultiplyPoint3x4(_localPositions[getIndex(fingerType, boneType)]);
      }

      private int getIndex(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return (int)fingerType * 4 + (int)boneType;
      }
    }
    #endregion
  }
}
