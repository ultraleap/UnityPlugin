/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  /**
  * The HoldingPoseControllerKabsch class implements a holding pose controller that  
  * solves for the position that best fits the object into the hand (using a Kabsch algorithm).
  *
  * Two solving methods are provided:
  *
  * * SixDegreeSolve -- allows the object to move and rotate in any direction.
  * * PivotAroundOrigin -- constrains the object to rotations only. Use this solving method
  *   for objects that cannot change position, such as a lever or attached wheel.
  * 
  * @since 4.1.4
  */
  public class HoldingPoseControllerKabsch : IHoldingPoseController {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    /** The defined solving methods. */
    public enum SolveMethod {
      SixDegreeSolve,
      PivotAroundOrigin
    }

    /** The specified solving method. */
    [Tooltip("The algorithm used to find the best holding position and rotation.")]
    [SerializeField]
    protected SolveMethod _solveMethod;

    protected Dictionary<int, HandPointCollection> _handIdToPoints;
    KabschSolver _kabsch;
    List<Vector3> points, refPoints;
    Vector3 handCentroid, objectCentroid; float boneCount;

    protected override void Init(InteractionBehaviour obj) {
      base.Init(obj);

      _handIdToPoints = new Dictionary<int, HandPointCollection>();
      _kabsch = new KabschSolver();
      points = new List<Vector3>(20); refPoints = new List<Vector3>(20);
    }

    public override void TransferHandId(int oldId, int newId) {
      _handIdToPoints[newId] = _handIdToPoints[oldId];
      _handIdToPoints.Remove(oldId);
    }

    public override void AddHand(Hand hand) {
      var newCollection = HandPointCollection.Create(_obj.warper);
      _handIdToPoints[hand.Id] = newCollection;

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

    public override void GetHoldingPose(ReadonlyList<Hand> hands, out Vector3 newPosition, out Quaternion newRotation) {
      points.Clear(); refPoints.Clear();
      Vector3 bodyPosition = _obj.warper.RigidbodyPosition;
      Quaternion bodyRotation = _obj.warper.RigidbodyRotation;
      Matrix4x4 it = Matrix4x4.TRS(bodyPosition, bodyRotation, Vector3.one);

      handCentroid = Vector3.zero; objectCentroid = Vector3.zero; boneCount = 0f;
      for (int h = 0; h < hands.Count; h++) {
        Hand hand = hands[h];

        var collection = _handIdToPoints[hand.Id];

        for (int f = 0; f < NUM_FINGERS; f++) {
          Finger finger = hand.Fingers[f];
          Finger.FingerType fingerType = finger.Type;

          for (int j = 0; j < NUM_BONES; j++) {
            Bone.BoneType boneType = (Bone.BoneType)j;
            Bone bone = finger.Bone(boneType);

            Vector3 localPos = collection.GetLocalPosition(fingerType, boneType);
            Vector3 bonePos = bone.NextJoint.ToVector3();

            //Do the solve such that the objects positions are matched to the new bone positions
            Vector3 point1 = (it.MultiplyPoint3x4(localPos) - bodyPosition);
            Vector3 point2 = (bonePos - bodyPosition);

            switch (_solveMethod) {
              case SolveMethod.SixDegreeSolve:
                //Set the relevant points in each array
                points.Add(point1); refPoints.Add(point2);
                break;
              case SolveMethod.PivotAroundOrigin:
                //Calculate the Centroids of the object and hand(s) points
                objectCentroid += point1;
                handCentroid += point2;
                boneCount += 1f;
                break;
            }
          }
        }
      }

      Matrix4x4 KabschTransform = performSolve(bodyPosition);

      newPosition = bodyPosition + KabschTransform.GetVector3();
      newRotation = KabschTransform.GetQuaternion() * bodyRotation;
    }

    
    protected Matrix4x4 performSolve(Vector3 position) {
      switch (_solveMethod) {
        case SolveMethod.SixDegreeSolve:
          return _kabsch.SolveKabsch(points, refPoints);
        case SolveMethod.PivotAroundOrigin:
          objectCentroid /= boneCount;
          handCentroid /= boneCount;
          if (!objectCentroid.Equals(handCentroid)) {
            return Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(objectCentroid, handCentroid), Vector3.one);
          }else {
            return Matrix4x4.identity;
          }
        default:
          return _kabsch.SolveKabsch(points, refPoints);
      }
    }

    protected class HandPointCollection {
      //Without a pool, you might end up with 2 instances per object
      //With a pool, likely there will only ever be 2 instances!
      private static Stack<HandPointCollection> _handPointCollectionPool = new Stack<HandPointCollection>();

      private Vector3[] _localPositions;
      private Matrix4x4 _inverseTransformMatrix;

      public static HandPointCollection Create(RigidbodyWarper warper) {
        HandPointCollection collection;
        if (_handPointCollectionPool.Count != 0) {
          collection = _handPointCollectionPool.Pop();
        } else {
          collection = new HandPointCollection();
        }

        collection.init(warper);
        return collection;
      }

      public static void Return(HandPointCollection handPointCollection) {
        _handPointCollectionPool.Push(handPointCollection);
      }

      private HandPointCollection() {
        _localPositions = new Vector3[NUM_FINGERS * NUM_BONES];
      }

      private void init(RigidbodyWarper warper) {
        Vector3 interactionPosition = warper.RigidbodyPosition;
        Quaternion interactionRotation = warper.RigidbodyRotation;
        _inverseTransformMatrix = Matrix4x4.TRS(interactionPosition, interactionRotation, Vector3.one).inverse;
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
