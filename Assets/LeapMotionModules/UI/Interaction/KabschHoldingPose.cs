using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class KabschHoldingPose : IHoldingPoseController {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    private InteractionBehaviour _interactionObj;
    private KabschSolver _kabsch;
    private List<Vector3> _points, _refPoints;

    // TODO: These variables are only used for PivotAroundOrigin calculation,
    // which is currently unused/unsupported
#pragma warning disable 0414 // remove these when decision is made
    private Vector3 _handCentroid, _objectCentroid;
    private float _boneCount;
#pragma warning restore 0414

    private Dictionary<InteractionHand, HandPointCollection> _handToPoints;

    public KabschHoldingPose(InteractionBehaviour interactionObj) {
      _interactionObj = interactionObj;
      // TODO: If an InteractionBehaviour is required for this anyway, why isn't that a part of the IHoldPositionBehaviour interface...

      _kabsch = new KabschSolver();
      _points = new List<Vector3>(20); _refPoints = new List<Vector3>(20);
      _handToPoints = new Dictionary<InteractionHand, HandPointCollection>();
    }

    public void AddHand(InteractionHand hand) {
      var newPoints = HandPointCollection.Create(_interactionObj.Rigidbody.position,
                                                     _interactionObj.Rigidbody.rotation);
      _handToPoints[hand] = newPoints;

      for (int f = 0; f < NUM_FINGERS; f++) {
        Finger finger = hand.GetLeapHand().Fingers[f];
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

    public void GetHoldingPose(out Vector3 newPosition, out Quaternion newRotation) {
      _points.Clear(); _refPoints.Clear();
      Vector3 bodyPosition = _interactionObj.Rigidbody.position;
      Quaternion bodyRotation = _interactionObj.Rigidbody.rotation;
      Matrix4x4 it = Matrix4x4.TRS(bodyPosition, bodyRotation, Vector3.one);

      _handCentroid = Vector3.zero; _objectCentroid = Vector3.zero; _boneCount = 0f;

      foreach (var handPointPair in _handToPoints) {
        InteractionHand hand = handPointPair.Key;
        Leap.Hand leapHand = hand.GetLeapHand();
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

            //switch (_solveMethod) {
            //  case SolveMethod.SixDegreeSolve:
               // Set the relevant points in each array

                // TODO: Determine whether supporting PivotAroundOrigin needs to happen
                _points.Add(point1); _refPoints.Add(point2);

            //    break;
            //  case SolveMethod.PivotAroundOrigin:
            //    //Calculate the Centroids of the object and hand(s) points
            //    objectCentroid += point1;
            //    handCentroid += point2;
            //    boneCount += 1f;
            //    break;
            //}
          }
        }
      }

      Matrix4x4 KabschTransform = PerformSolve(bodyPosition);

      newPosition = bodyPosition + KabschTransform.GetVector3();
      newRotation = KabschTransform.GetQuaternion() * bodyRotation;
    }

    private Matrix4x4 PerformSolve(Vector3 position) {
      //switch (_solveMethod) {
      //  case SolveMethod.SixDegreeSolve:

          // TODO: Determine whether supporting PivotAroundOrigin needs to happen, as above
          return _kabsch.SolveKabsch(_points, _refPoints);

      //  case SolveMethod.PivotAroundOrigin:
      //    objectCentroid /= boneCount;
      //    handCentroid /= boneCount;
      //    if (!objectCentroid.Equals(handCentroid)) {
      //      return Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(objectCentroid, handCentroid), Vector3.one);
      //    }
      //    else {
      //      return Matrix4x4.identity;
      //    }
      //  default:
      //    return _kabsch.SolveKabsch(points, refPoints);
      //}
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