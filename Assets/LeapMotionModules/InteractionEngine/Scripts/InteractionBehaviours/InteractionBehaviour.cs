using UnityEngine;
using System;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : InteractionBehaviourBase {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    [Tooltip("A transform that acts as the parent of all renderers for this object.  By seperating out the graphical " +
             "representation from the physical, interaction fidelity can be improved and latency reduced.")]
    [SerializeField]
    protected Transform _graphicalAnchor;

    [Tooltip("Should a hand be able to impart pushing forces to this object.")]
    [SerializeField]
    protected bool _pushingEnabled = true;

    protected Renderer[] _renderers;
    protected Rigidbody _rigidbody;

    protected bool _isKinematic;
    protected bool _useGravity;
    protected bool _recievedVelocityUpdate = false;
    protected bool _notifiedOfTeleport = false;
    
    protected Vector3 _accumulatedLinearAcceleration = Vector3.zero;
    protected Vector3 _accumulatedAngularAcceleration = Vector3.zero;

    protected Dictionary<int, HandPointCollection> _handIdToPoints;
    protected LEAP_IE_KABSCH _kabsch;

    protected Bounds _debugBounds;

    #region PUBLIC METHODS
    public void AddLinearAcceleration(Vector3 acceleration) {
      _accumulatedLinearAcceleration += acceleration;
    }

    public void AddAngularAcceleration(Vector3 acceleration) {
      _accumulatedAngularAcceleration += acceleration;
    }

    public void NotifyTeleported() {
      _notifiedOfTeleport = true;
    }
    #endregion

    #region INTERACTION CALLBACKS

    public override void OnRegister() {
      base.OnRegister();

      _rigidbody = GetComponent<Rigidbody>();
      if (_rigidbody == null) {
        throw new InvalidOperationException("InteractionBehaviour must have a Rigidbody component attached to it.");
      }

      _isKinematic = _rigidbody.isKinematic;
      _useGravity = _rigidbody.useGravity;

      //Gravity is always manually applied
      _rigidbody.useGravity = false;

      KabschC.Construct(ref _kabsch);
    }

    public override void OnUnregister() {
      base.OnUnregister();

      _rigidbody.useGravity = _useGravity;
      KabschC.Destruct(ref _kabsch);
    }

    public override void OnPostSolve() {
      base.OnPreSolve();

      if (!_recievedVelocityUpdate) {
        _rigidbody.AddForce(_accumulatedLinearAcceleration, ForceMode.Acceleration);
        _rigidbody.AddTorque(_accumulatedAngularAcceleration, ForceMode.Acceleration);
        if (_useGravity) {
          _rigidbody.AddForce(Physics.gravity, ForceMode.Acceleration);
        }
      }

      _accumulatedLinearAcceleration = Vector3.zero;
      _accumulatedAngularAcceleration = Vector3.zero;

      _notifiedOfTeleport = false;
      _recievedVelocityUpdate = false;
    }

    public override void OnInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform) {
      createInfo = new INTERACTION_CREATE_SHAPE_INFO();
      createInfo.gravity = Physics.gravity.ToCVector();
      createInfo.shapeFlags = ShapeInfoFlags.None;

      if (!_isKinematic) {
        createInfo.shapeFlags |= ShapeInfoFlags.HasRigidBody;
      }

      if (_useGravity) {
        createInfo.shapeFlags |= ShapeInfoFlags.GravityEnabled;
      }

      createTransform = getRigidbodyTransform();
    }

    public override void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      base.OnInteractionShapeCreated(instanceHandle);

#if UNITY_EDITOR
      Collider[] colliders = GetComponentsInChildren<Collider>();
      _debugBounds = colliders[0].bounds;
      for (int i = 1; i < colliders.Length; i++) {
        _debugBounds.Encapsulate(colliders[i].bounds);
      }
      _debugBounds.center = transform.InverseTransformPoint(_debugBounds.center);
#endif
    }

    public override void OnInteractionShapeUpdate(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTrasnform) {
      updateInfo = new INTERACTION_UPDATE_SHAPE_INFO();
      updateInfo.updateFlags = _notifiedOfTeleport ? UpdateInfoFlags.ResetVelocity : UpdateInfoFlags.ApplyAcceleration;
      updateInfo.linearAcceleration = _accumulatedLinearAcceleration.ToCVector();
      updateInfo.angularAcceleration = _accumulatedAngularAcceleration.ToCVector();

      interactionTrasnform = getRigidbodyTransform();
    }

    public override void OnRecieveSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      base.OnRecieveSimulationResults(results);

      if ((results.resultFlags & ShapeInstanceResultFlags.Velocities) != 0) {
        _rigidbody.Sleep();
        _rigidbody.velocity = results.linearVelocity.ToVector3();
        _rigidbody.angularVelocity = results.angularVelocity.ToVector3();
        _recievedVelocityUpdate = true;
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
      if (_notifiedOfTeleport) {
        _rigidbody.position = newPosition;
        _rigidbody.rotation = newRotation;
      } else {
        _rigidbody.MovePosition(newPosition);
        _rigidbody.MoveRotation(newRotation);
      }

      _graphicalAnchor.position = newPosition;
      _graphicalAnchor.rotation = newRotation;
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

      NotifyTeleported();
    }

    public override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);

      updateRendererStatus();
      removeHandPointCollection(oldHand.Id);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();

      if (!_isKinematic) {
        _rigidbody.isKinematic = true;
      }
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();

      if (!_isKinematic) {
        _rigidbody.isKinematic = false;
      }
    }
    #endregion

    #region UNITY CALLBACKS
    protected virtual void Awake() {
      _handIdToPoints = new Dictionary<int, HandPointCollection>();
    }

    protected virtual void OnDrawGizmos() {
      Matrix4x4 gizmosMatrix = Gizmos.matrix;

      Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
      Gizmos.color = IsBeingGrasped ? Color.green : Color.blue;
      Gizmos.DrawWireCube(_debugBounds.center, _debugBounds.size);

      Gizmos.matrix = gizmosMatrix;
    }
    #endregion

    #region INTERNAL
    protected INTERACTION_TRANSFORM getRigidbodyTransform() {
      INTERACTION_TRANSFORM interactionTransform = new INTERACTION_TRANSFORM();
      interactionTransform.position = _rigidbody.position.ToCVector();
      interactionTransform.rotation = _rigidbody.rotation.ToCQuaternion();
      interactionTransform.wallTime = Time.fixedTime;
      return interactionTransform;
    }

    protected virtual void updateRendererStatus() {
      //Renderers are visible if there are no grasping hands
      //or if there is at least one tracked grasping hand
      int trackedGraspingHandCount = GraspingHandCount - UntrackedHandCount;
      bool shouldBeVisible = GraspingHandCount == 0 || trackedGraspingHandCount > 0;

      _graphicalAnchor.gameObject.SetActive(shouldBeVisible);
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
