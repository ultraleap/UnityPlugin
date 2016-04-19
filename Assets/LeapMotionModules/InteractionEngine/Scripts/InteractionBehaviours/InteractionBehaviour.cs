using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  [SelectionBase]
  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : InteractionBehaviourBase {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    [Tooltip("A transform that acts as the parent of all renderers for this object.  By seperating out the graphical " +
             "representation from the physical, interaction fidelity can be improved and latency reduced.")]
    [SerializeField]
    protected Transform _graphicalAnchor;

    [Tooltip("How long it takes for the graphical anchor to return to the origin after a release.")]
    [SerializeField]
    protected float _graphicalReturnTime = 0.25f;

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

    private Coroutine _graphicalLerpCoroutine = null;

    private Bounds _debugBounds;

    #region PUBLIC METHODS

    public bool IsKinematic {
      get {
        return _isKinematic;
      }
      set {
        _isKinematic = value;
        if (!IsRegisteredWithManager) {
          _rigidbody.isKinematic = value;
        }
      }
    }

    public bool UseGravity {
      get {
        return _useGravity;
      }
      set {
        _useGravity = value;
        if (!IsRegisteredWithManager) {
          _rigidbody.useGravity = _useGravity;
        }
      }
    }

    public Transform GraphicalAnchor {
      get {
        return _graphicalAnchor;
      }
      set {
        if (_graphicalLerpCoroutine != null) {
          StopCoroutine(_graphicalLerpCoroutine);
          _graphicalLerpCoroutine = null;
        }

        if (_graphicalAnchor != null) {
          _graphicalAnchor.gameObject.SetActive(true);
        }

        _graphicalAnchor = value;

        updateRendererStatus();
      }
    }

    public float GraphicalReturnTime {
      get {
        return _graphicalReturnTime;
      }
      set {
        _graphicalReturnTime = value;
      }
    }

    public bool PushingEnabled {
      get {
        return _pushingEnabled;
      }
      set {
        _pushingEnabled = value;
      }
    }

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

    protected override void OnRegistered() {
      base.OnRegistered();

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

    protected override void OnUnregistered() {
      base.OnUnregistered();

      _rigidbody.isKinematic = _isKinematic;
      _rigidbody.useGravity = _useGravity;
      KabschC.Destruct(ref _kabsch);
    }

    protected override void OnPostSolve() {
      base.OnPostSolve();

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

    public override void GetInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform) {
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

    protected override void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      base.OnInteractionShapeCreated(instanceHandle);

#if UNITY_EDITOR
      Collider[] colliders = GetComponentsInChildren<Collider>();
      if (colliders.Length > 0) {
        _debugBounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++) {
          _debugBounds.Encapsulate(colliders[i].bounds);
        }
        _debugBounds.center = transform.InverseTransformPoint(_debugBounds.center);
      }
#endif
    }

    public override void GetInteractionShapeUpdateInfo(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTransform) {
      updateInfo = new INTERACTION_UPDATE_SHAPE_INFO();
      updateInfo.updateFlags = UpdateInfoFlags.ExplicitVelocity;
      updateInfo.updateFlags |= _notifiedOfTeleport ? UpdateInfoFlags.ResetVelocity : UpdateInfoFlags.None;
      updateInfo.updateFlags |= _pushingEnabled && !_isKinematic ? UpdateInfoFlags.ApplyAcceleration : UpdateInfoFlags.None;
      
      updateInfo.linearAcceleration = _accumulatedLinearAcceleration.ToCVector();
      updateInfo.angularAcceleration = _accumulatedAngularAcceleration.ToCVector();
      updateInfo.linearVelocity = _rigidbody.velocity.ToCVector();
      updateInfo.angularVelocity = _rigidbody.angularVelocity.ToCVector();

      interactionTransform = getRigidbodyTransform();
    }

    protected override void OnRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      base.OnRecievedSimulationResults(results);

      if ((results.resultFlags & ShapeInstanceResultFlags.Velocities) != 0) {
        _rigidbody.Sleep();
        _rigidbody.velocity = results.linearVelocity.ToVector3();
        _rigidbody.angularVelocity = results.angularVelocity.ToVector3();
        _recievedVelocityUpdate = true;
      }
    }

    protected override void OnHandGrasped(Hand hand) {
      base.OnHandGrasped(hand);

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
          Vector3 closestOnSurface = bonePos;

          newCollection.SetGlobalPosition(closestOnSurface, fingerType, boneType);
        }
      }
    }

    protected override void OnHandsHoldPhysics(List<Hand> hands) {
      base.OnHandsHoldPhysics(hands);

      //Get new transform
      Vector3 newPosition;
      Quaternion newRotation;
      getSolvedTransform(hands, out newPosition, out newRotation);

      //Apply new transform to object
      if (_notifiedOfTeleport) {
        _rigidbody.position = newPosition;
        _rigidbody.rotation = newRotation;
      } else {
        _rigidbody.MovePosition(newPosition);
        _rigidbody.MoveRotation(newRotation);
      }
    }

    protected override void OnHandsHoldGraphics(List<Hand> hands) {
      base.OnHandsHoldGraphics(hands);

      if (_graphicalAnchor != null) {
        //Get new transform
        Vector3 newPosition;
        Quaternion newRotation;
        getSolvedTransform(hands, out newPosition, out newRotation);

        _graphicalAnchor.position = newPosition;
        _graphicalAnchor.rotation = newRotation;
      }
    }

    protected override void OnHandReleased(Hand hand) {
      base.OnHandReleased(hand);

      removeHandPointCollection(hand.Id);
    }

    protected override void OnHandLostTracking(Hand oldHand) {
      base.OnHandLostTracking(oldHand);

      updateRendererStatus();
    }

    protected override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);

      updateRendererStatus();

      //Associate the collection with the new id
      var collection = _handIdToPoints[oldId];
      _handIdToPoints.Remove(oldId);
      _handIdToPoints[newHand.Id] = collection;

      NotifyTeleported();
    }

    protected override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);

      updateRendererStatus();
      removeHandPointCollection(oldHand.Id);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();

      if (_graphicalLerpCoroutine != null) {
        StopCoroutine(_graphicalLerpCoroutine);
      }

      if (!_isKinematic) {
        _rigidbody.isKinematic = true;
      }
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();

      if (_graphicalAnchor != null) {
        _graphicalLerpCoroutine = StartCoroutine(lerpGraphicalToOrigin());
      }

      if (!_isKinematic) {
        _rigidbody.isKinematic = false;
      }
    }
    #endregion

    #region UNITY CALLBACKS
    protected virtual void Awake() {
      _handIdToPoints = new Dictionary<int, HandPointCollection>();
    }

    protected IEnumerator lerpGraphicalToOrigin() {
      Vector3 startOffset = _graphicalAnchor.position - transform.position;
      Quaternion startRot = _graphicalAnchor.localRotation;
      float startTime = Time.time;
      while (true) {
        yield return null;

        float t = Mathf.InverseLerp(startTime, startTime + _graphicalReturnTime, Time.time);
        float percent = t * t * (3 - 2 * t);

        _graphicalAnchor.position = transform.position + Vector3.Lerp(startOffset, Vector3.zero, percent);
        _graphicalAnchor.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, percent);

        if (percent >= 1.0f) {
          break;
        }
      }

      _graphicalLerpCoroutine = null;
    }

    protected virtual void OnDrawGizmos() {
      if (IsRegisteredWithManager) {
        Matrix4x4 gizmosMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = IsBeingGrasped ? Color.green : Color.blue;
        Gizmos.DrawWireCube(_debugBounds.center, _debugBounds.size);

        Gizmos.matrix = gizmosMatrix;
      }
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

      if (_graphicalAnchor != null) {
        _graphicalAnchor.gameObject.SetActive(shouldBeVisible);
      }
    }

    protected void removeHandPointCollection(int handId) {
      var collection = _handIdToPoints[handId];
      _handIdToPoints.Remove(handId);
      HandPointCollection.Return(collection);
    }

    protected void getSolvedTransform(List<Hand> hands, out Vector3 newPosition, out Quaternion newRotation) {
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

      KabschC.Solve(ref _kabsch);

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
