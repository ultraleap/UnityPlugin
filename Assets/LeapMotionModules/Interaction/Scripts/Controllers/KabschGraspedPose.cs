/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class KabschGraspedPose : IGraspedPoseController {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    public enum SolveMethod {
      SixDegreeSolve,
      PivotAroundOrigin
    }
    private SolveMethod _solveMethod;

    private InteractionBehaviour _interactionObj;
    private KabschSolver _kabsch;
    private List<Vector3> _points, _refPoints;

    private Vector3 _handCentroid, _objectCentroid;
    private float _boneCount;

    private Dictionary<InteractionHand, HandPointCollection> _handToPoints;

    public KabschGraspedPose(InteractionBehaviour interactionObj) {
      _interactionObj = interactionObj;
      // TODO: If an InteractionBehaviour is required for this anyway, why isn't that a part of the IHoldPositionBehaviour interface...

      _kabsch = new KabschSolver();
      _points = new List<Vector3>(20); _refPoints = new List<Vector3>(20);
      _handToPoints = new Dictionary<InteractionHand, HandPointCollection>();
    }

    public void AddHand(InteractionHand hand) {
      var newPoints = HandPointCollection.Create(_interactionObj.rigidbody.position,
                                                 _interactionObj.rigidbody.rotation);
      _handToPoints[hand] = newPoints;

      for (int f = 0; f < NUM_FINGERS; f++) {
        Finger finger = hand.GetLastTrackedLeapHand().Fingers[f];
        Finger.FingerType fingerType = finger.Type;

        for (int j = 0; j < NUM_BONES; j++) {
          Bone.BoneType boneType = (Bone.BoneType)j;
          Bone bone = finger.Bone(boneType);

          Vector3 bonePos = bone.NextJoint.ToVector3();

          // Global position of the point is just the position of the joint itself.
          newPoints.SetGlobalPosition(bonePos, fingerType, boneType);
        }
      }
    }

    public void RemoveHand(InteractionHand hand) {
      var collection = _handToPoints[hand];
      _handToPoints.Remove(hand);

      // Return the collection to the pool so it can be re-used.
      HandPointCollection.Return(collection);
    }

    public void ClearHands() {
      foreach (var handPointsPair in _handToPoints) {
        HandPointCollection.Return(handPointsPair.Value);
      }
      _handToPoints.Clear();
    }

    public void GetGraspedPosition(out Vector3 newPosition, out Quaternion newRotation) {
      _points.Clear(); _refPoints.Clear();
      Vector3 bodyPosition = _interactionObj.rigidbody.position;
      Quaternion bodyRotation = _interactionObj.rigidbody.rotation;
      Matrix4x4 it = Matrix4x4.TRS(bodyPosition, bodyRotation, Vector3.one);

      _handCentroid = Vector3.zero; _objectCentroid = Vector3.zero; _boneCount = 0f;

      foreach (var handPointPair in _handToPoints) {
        InteractionHand hand = handPointPair.Key;
        Leap.Hand leapHand = hand.GetLastTrackedLeapHand();
        HandPointCollection points = _handToPoints[hand];

        for (int f = 0; f < NUM_FINGERS; f++) {
          Finger finger = leapHand.Fingers[f];
          Finger.FingerType fingerType = finger.Type;

          for (int j = 0; j < NUM_BONES; j++) {
            Bone.BoneType boneType = (Bone.BoneType)j;
            Bone bone = finger.Bone(boneType);

            Vector3 localPos = points.GetLocalPosition(fingerType, boneType);
            Vector3 bonePos = bone.NextJoint.ToVector3();

            //Do the solve such that the objects positions are matched to the new bone positions
            Vector3 point1 = (it.MultiplyPoint3x4(localPos) - bodyPosition);
            Vector3 point2 = (bonePos - bodyPosition);

            if (_interactionObj.isPositionLocked) {
              // Only rotate the object, pivoting around its origin.
              _solveMethod = SolveMethod.PivotAroundOrigin;
              _objectCentroid += point1;
              _handCentroid += point2;
              _boneCount += 1f;
            }
            else {
              // Do normal Kabsch solve.
              _solveMethod = SolveMethod.SixDegreeSolve;
              _points.Add(point1); _refPoints.Add(point2);
            }
          }
        }
      }

      Matrix4x4 KabschTransform = PerformSolve(bodyPosition);

      newPosition = bodyPosition + KabschTransform.GetVector3();
      newRotation = KabschTransform.GetQuaternion() * bodyRotation;
    }

    private Matrix4x4 PerformSolve(Vector3 position) {
      switch (_solveMethod) {
        case SolveMethod.SixDegreeSolve:

          // TODO: Determine whether supporting PivotAroundOrigin needs to happen, as above
          return _kabsch.SolveKabsch(_points, _refPoints);

        case SolveMethod.PivotAroundOrigin:
          _objectCentroid /= _boneCount;
          _handCentroid /= _boneCount;
          if (!_objectCentroid.Equals(_handCentroid)) {
            return Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(_objectCentroid, _handCentroid), Vector3.one);
          }
          else {
            return Matrix4x4.identity;
          }
        default:
          return _kabsch.SolveKabsch(_points, _refPoints);
      }
    }

    protected class HandPointCollection {
      //Without a pool, you might end up with 2 instances per object
      //With a pool, likely there will only ever be 2 instances!
      private static Stack<HandPointCollection> _handPointCollectionPool = new Stack<HandPointCollection>();

      private Vector3[] _localPositions;
      private Matrix4x4 _inverseTransformMatrix;

      public static HandPointCollection Create(Vector3 position, Quaternion rotation) {
        HandPointCollection collection;
        if (_handPointCollectionPool.Count != 0) {
          collection = _handPointCollectionPool.Pop();
        } else {
          collection = new HandPointCollection();
        }

        collection.Initialize(position, rotation);
        return collection;
      }

      public static void Return(HandPointCollection handPointCollection) {
        _handPointCollectionPool.Push(handPointCollection);
      }

      private HandPointCollection() {
        _localPositions = new Vector3[NUM_FINGERS * NUM_BONES];
      }

      private void Initialize(Vector3 position, Quaternion rotation) {
        _inverseTransformMatrix = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
      }

      public void SetGlobalPosition(Vector3 globalPosition, Finger.FingerType fingerType, Bone.BoneType boneType) {
        _localPositions[getIndex(fingerType, boneType)] = _inverseTransformMatrix.MultiplyPoint3x4(globalPosition);
      }

      public Vector3 GetLocalPosition(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return _localPositions[getIndex(fingerType, boneType)];
      }

      private int getIndex(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return (int)fingerType * 4 + (int)boneType;
      }
    }

  }

}
