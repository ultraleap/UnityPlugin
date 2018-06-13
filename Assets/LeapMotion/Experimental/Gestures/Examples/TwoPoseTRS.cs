using Leap.Unity;
using Leap.Unity.Attributes;
using Leap.Unity.Gestures;
using Leap.Unity.Infix;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

using Pose = Leap.Unity.Pose;

namespace Leap.Unity.Gestures {

  public class TwoPoseTRS : MonoBehaviour, IRuntimeGizmoComponent {

    #region Inspector

    [Header("Transform To Manipulate")]

    public Transform objectTransform;

    [Header("Pose Gestures")]

    [SerializeField]
    [ImplementsInterface(typeof(IPoseGesture))]
    private MonoBehaviour _poseSourceA;
    public IPoseGesture poseSourceA { get {  return _poseSourceA as IPoseGesture; } }

    [SerializeField]
    [ImplementsInterface(typeof(IPoseGesture))]
    private MonoBehaviour _poseSourceB;
    public IPoseGesture poseSourceB { get { return _poseSourceB as IPoseGesture; } }

    [Header("Scale")]

    [SerializeField]
    private bool _allowScale = true;

    [SerializeField]
    private float _minScale = 0.02f;

    [SerializeField]
    private float _maxScale = 50f;

    [Header("Position Constraint")]
    
    [SerializeField]
    [Tooltip("Constrains the maximum distance from the TRS object, only if the user isn't "
          + "actively performing the TRS action.")]
    private bool _constrainPosition = true;

    [SerializeField]
    [Tooltip("The maximum distance from the base position in the OBJECT'S space.")]
    private float _constraintDistance = 60f;

    [SerializeField]
    [Tooltip("The maximum distance from the base position in world space.")]
    private float _maxWorldDistance = 30f;

    [SerializeField]
    [DisableIf("_constrainPosition", isEqualTo: false)]
    private float _constraintStrength = 4f;

    [SerializeField]
    private float _maxConstraintSpeed = 1f;

    [SerializeField, Disable]
    private Vector3 _basePosition = Vector3.zero;

    [Tooltip("The base transform provides a dynamic base position if non-null. Since the "
          + "user is usually the entity that wants to not drift too far away, this will "
          + "default to the main camera if there is one.")]
    [SerializeField]
    private Transform _baseTransform;

    [Tooltip("Readonly. The local distance from the object to the base.")]
    [SerializeField, Disable]
    private float _localDistanceFromBase = 1f;

    [Header("Momentum (when not pinching)")]

    [SerializeField]
    private bool _allowMomentum = true;
    public bool allowMomentum {
      get { return _allowMomentum; }
      set { _allowMomentum = value; }
    }

    [SerializeField]
    private float      _linearFriction = 8f;

    [SerializeField]
    private float      _angularFriction = 0.9f;

    [SerializeField]
    private float      _scaleFriction = 25f;

    [SerializeField, Disable]
    private Vector3    _positionMomentum;

    [SerializeField, Disable]
    private Vector3    _rotationMomentum;

    [SerializeField, Disable, MinValue(0.001f)]
    private float      _scaleMomentum = 1f;

    [SerializeField, Disable]
    private Vector3   _lastKnownCentroid = Vector3.zero;

    [Header("Debug Runtime Gizmos")]

    [SerializeField]
    private bool _drawDebug = false;
    public bool drawDebug {
      get { return _drawDebug; }
      set { _drawDebug = value; }
    }

    [SerializeField]
    private bool _drawPositionConstraints = false;

    #endregion

    #region Unity Events

    private void Reset() {
      if (_baseTransform == null) {
        var mainCamera = Camera.main;
        if (mainCamera != null) {
          _baseTransform = Camera.main.transform;
        }
      }
    }

    void Update() {
      updateTRS();
    }

    #endregion

    #region TRS

    private RingBuffer<Pose> _aPoses = new RingBuffer<Pose>(2);
    private RingBuffer<Pose> _bPoses = new RingBuffer<Pose>(2);

    private void updateTRS() {
      
      // Get basic grab switch state information.
      var aGrasped = poseSourceA != null && poseSourceA.isActive;
      var bGrasped = poseSourceB != null && poseSourceB.isActive;

      int numGrasping = (aGrasped? 1 : 0) + (bGrasped ? 1 : 0);

      if (!aGrasped) {
        _aPoses.Clear();
      }
      else {
        _aPoses.Add(poseSourceA.pose);
      }

      if (!bGrasped) {
        _bPoses.Clear();
      }
      else {
        _bPoses.Add(poseSourceB.pose);
      }

      // Declare information for applying the TRS.
      var objectScale = objectTransform.localScale.x;
      var origCentroid = Vector3.zero;
      var nextCentroid = Vector3.zero;
      var origAxis = Vector3.zero;
      var nextAxis = Vector3.zero;
      var twist = 0f;
      var applyPositionalMomentum = false;
      var applyRotateScaleMomentum = false;
      var applyPositionConstraint = false;

      // Fill information based on grab switch state.
      if (numGrasping == 0) {
        applyPositionalMomentum  = true;
        applyRotateScaleMomentum = true;
        applyPositionConstraint = true;
      }
      else if (numGrasping == 1) {

        var poses = aGrasped ? _aPoses : (bGrasped ? _bPoses : null);

        if (poses != null && poses.IsFull) {

          // Translation.
          origCentroid = poses[0].position;
          nextCentroid = poses[1].position;

        }

        applyRotateScaleMomentum = true;

        _lastKnownCentroid = nextCentroid;
      }
      else {

        if (_aPoses.IsFull && _bPoses.IsFull) {

          // Scale changes.
          float dist0 = Vector3.Distance(_aPoses[0].position, _bPoses[0].position);
          float dist1 = Vector3.Distance(_aPoses[1].position, _bPoses[1].position);
          
          float scaleChange = dist1 / dist0;
          
          if (_allowScale && !float.IsNaN(scaleChange) && !float.IsInfinity(scaleChange)) {
            objectScale *= scaleChange;
          }

          // Translation.
          origCentroid = (_aPoses[0].position + _bPoses[0].position) / 2f;
          nextCentroid = (_aPoses[1].position + _bPoses[1].position) / 2f;

          // Axis rotation.
          origAxis = (_bPoses[0].position - _aPoses[0].position);
          nextAxis = (_bPoses[1].position - _aPoses[1].position);

          // Twist.
          var perp = Utils.Perpendicular(nextAxis);
          
          //var aRotatedPerp = perp.RotatedBy(_aPoses[0].rotation.From(_aPoses[1].rotation));
          var aRotatedPerp = (_aPoses[1].rotation * Quaternion.Inverse(_aPoses[0].rotation))
                            * perp;
          var aTwist = Vector3.SignedAngle(perp, aRotatedPerp, nextAxis);

          //var bRotatedPerp = perp.RotatedBy(_bPoses[0].rotation.From(_bPoses[1].rotation));
          var bRotatedPerp = (_bPoses[1].rotation * Quaternion.Inverse(_bPoses[0].rotation))
                            * perp;
          var bTwist = Vector3.SignedAngle(perp, bRotatedPerp, nextAxis);

          twist = (aTwist + bTwist) * 0.5f;

          _lastKnownCentroid = nextCentroid;
        }
      }


      // Calculate TRS.
      Vector3    origTargetPos = objectTransform.transform.position;
      Quaternion origTargetRot = objectTransform.transform.rotation;

      // No references to this as of 2/15. Is it important? -Nick
      //float      origTargetScale = objectTransform.transform.localScale.x;

      // Declare delta properties.
      Vector3    finalPosDelta;
      Quaternion finalRotDelta;
      float      finalScaleRatio;

      // Translation: apply momentum, or just apply the translation and record momentum.
      finalPosDelta = (nextCentroid - origCentroid);

      // Determine base position if we expect to apply positional constraints.
      float distanceToEdge = 0f;
      if (_constrainPosition) {
        if (_baseTransform) {
          _basePosition = _baseTransform.position;
        }
        else if (objectTransform.parent != null) {
          _basePosition = objectTransform.parent.position;
        }
        else {
          _basePosition = Vector3.zero;
        }

        var worldDistanceFromBase = Vector3.Distance(_basePosition, objectTransform.position);

        _localDistanceFromBase = objectTransform.InverseTransformVector(
                                  worldDistanceFromBase
                                  * Vector3.right).magnitude;

        distanceToEdge = Mathf.Max(0f, _localDistanceFromBase - _constraintDistance);

        if (distanceToEdge == 0f) {
          var worldDistanceToEdge = Mathf.Max(0f, worldDistanceFromBase - _maxWorldDistance);
          distanceToEdge = objectTransform.InverseTransformVector(
                            worldDistanceToEdge
                            * Vector3.right).magnitude;
        }

      }

      // Apply momentum if necessary, otherwise we'll perform direct TRS.
      if ((_allowMomentum && applyPositionalMomentum)
          || (applyPositionalMomentum && _constrainPosition && distanceToEdge > 0f)) {
        
        // Constrain momentum to constrain the object's position if necessary.
        if (_constrainPosition && applyPositionConstraint) {
          var constraintDir = (_basePosition - objectTransform.position).normalized;

          // If we're not allowed to have normal momentum, immediately cancel any momentum
          // that isn't part of the constraint momentum.
          if (!_allowMomentum) {
            _positionMomentum = Vector3.ClampMagnitude(_positionMomentum,
                                  Mathf.Max(0f, Vector3.Dot(_positionMomentum, constraintDir)));
          }

          var constraintMomentum = distanceToEdge * _constraintStrength * 0.0005f
                                  * constraintDir;

          constraintMomentum = Vector3.ClampMagnitude(constraintMomentum,
                                            _maxConstraintSpeed * Time.deltaTime);

          _positionMomentum = Vector3.Lerp(_positionMomentum, constraintMomentum,
                                          2f * Time.deltaTime);
        }

        // Apply (and decay) momentum.
        objectTransform.position += _positionMomentum;

        var _frictionDir = -_positionMomentum.normalized;
        _positionMomentum += _frictionDir * _positionMomentum.magnitude
                                          * _linearFriction
                                          * Time.deltaTime;

        // Also apply some drag so we never explode...
        _positionMomentum += (_frictionDir) * _positionMomentum.sqrMagnitude * _linearFriction * 0.1f;
      }
      else {
        // Apply transformation!
        objectTransform.position = objectTransform.position + finalPosDelta;

        // Measure momentum only.
        _positionMomentum = Vector3.Lerp(_positionMomentum, finalPosDelta, 20f * Time.deltaTime);
      }

      // Remember last known centroid as pivot; remember local offset, scale, rotation,
      // then correct.
      var centroid = _lastKnownCentroid;
      var centroid_local = objectTransform.worldToLocalMatrix.MultiplyPoint3x4(centroid);
      
      // Scale.
      finalScaleRatio = objectScale / objectTransform.localScale.x;

      // Rotation.
      var poleRotation = Quaternion.FromToRotation(origAxis, nextAxis);
      var poleTwist = Quaternion.AngleAxis(twist, nextAxis);
      // Deprecated "From/Then"-style expression, original code:
      // finalRotDelta = objectTransform.rotation
      //                                .Then(poleTwist)
      //                                .Then(poleRotation)
      //                                .From(objectTransform.rotation);
      // Equivalent of the above post-deprecation -- could have out-of-order bug?
      finalRotDelta = objectTransform.rotation.Inverse()
                      * (objectTransform.rotation
                         * poleTwist
                         * poleRotation);


      // Apply scale and rotation, or use momentum for these properties.
      if (_allowMomentum && applyRotateScaleMomentum) {
        // Apply (and decay) momentum only.
        objectTransform.rotation = objectTransform.rotation
                                   * Quaternion.AngleAxis(_rotationMomentum.magnitude,
                                                          _rotationMomentum.normalized);
        objectTransform.localScale *= _scaleMomentum;

        var rotationFrictionDir = -_rotationMomentum.normalized;
        _rotationMomentum += rotationFrictionDir * _rotationMomentum.magnitude
                                                * _angularFriction
                                                * Time.deltaTime;
        // Also add some angular drag.
        _rotationMomentum += rotationFrictionDir * _rotationMomentum.sqrMagnitude * _angularFriction * 0.1f;
        _rotationMomentum = Vector3.Lerp(_rotationMomentum, Vector3.zero, _angularFriction * 5f * Time.deltaTime);

        _scaleMomentum = Mathf.Lerp(_scaleMomentum, 1f, _scaleFriction * Time.deltaTime);
      }
      else {
        // Apply transformations.
        objectTransform.rotation = objectTransform.rotation * finalRotDelta;
        objectTransform.localScale = Vector3.one * (objectTransform.localScale.x
                                                    * finalScaleRatio);

        // Measure momentum only.
        _rotationMomentum = Vector3.Lerp(_rotationMomentum, finalRotDelta.ToAngleAxisVector(), 40f * Time.deltaTime);
        _scaleMomentum = Mathf.Lerp(_scaleMomentum, finalScaleRatio, 20f * Time.deltaTime);
      }

      // Apply scale constraints.
      if (objectTransform.localScale.x < _minScale) {
        objectTransform.localScale = _minScale * Vector3.one;
        _scaleMomentum = 1f;
      }
      else if (objectTransform.localScale.x > _maxScale) {
        objectTransform.localScale = _maxScale * Vector3.one;
        _scaleMomentum = 1f;
      }

      // Restore centroid pivot.
      var movedCentroid = objectTransform.localToWorldMatrix.MultiplyPoint3x4(centroid_local);
      objectTransform.position += (centroid - movedCentroid);
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!_drawDebug) return;

      if (objectTransform == null) return;

      drawer.PushMatrix();
      drawer.matrix = objectTransform.localToWorldMatrix;

      drawer.color = LeapColor.coral;
      drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.20f);

      drawer.color = LeapColor.jade;
      drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.10f);

      drawer.PopMatrix();

      if (_constrainPosition && _drawPositionConstraints) { 
        drawer.color = LeapColor.lavender;
        var dir = (Camera.main.transform.position - _basePosition).normalized;
        drawer.DrawWireSphere(_basePosition,
                              objectTransform.TransformVector(Vector3.right * _constraintDistance).magnitude);

        drawer.color = LeapColor.violet;
        drawer.DrawWireSphere(_basePosition,
                              _maxWorldDistance);
      }
    }

    #endregion

  }
  
}
