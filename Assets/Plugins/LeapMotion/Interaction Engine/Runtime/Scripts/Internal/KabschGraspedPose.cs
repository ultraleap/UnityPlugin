/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// This pose handler is the default implementation of IGraspedPoseHandler provided by
  /// the Interaction Engine and is most likely the only implementation you will need. It
  /// uses a Kabsch solve from frame to frame based on the points at which any grasping
  /// controller are grasping the interaction object to determine where the object should
  /// move and rotate in the grasp. Note that IGraspedPoseHandlers only determine the
  /// target position and rotation of a held object; actually moving the object is the
  /// domain of an IGraspedMovementHandler.
  /// </summary>
  public class KabschGraspedPose : IGraspedPoseHandler {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    public enum SolveMethod {
      SixDegreeSolve,
      PivotAroundOrigin
    }
    private SolveMethod _solveMethod;

    private InteractionBehaviour _intObj;
    private KabschSolver _kabsch;
    private List<Vector3> _points, _refPoints;

    private Vector3 _controllerCentroid, _objectCentroid;
    private float _manipulatorCount;

    private Dictionary<InteractionController, PosePointCollection> _controllerToPoints;

    public KabschGraspedPose(InteractionBehaviour interactionObj) {
      _intObj = interactionObj;

      _kabsch = new KabschSolver();
      _points = new List<Vector3>(20); _refPoints = new List<Vector3>(20);
      _controllerToPoints = new Dictionary<InteractionController, PosePointCollection>();
    }

    public void AddController(InteractionController controller) {
      var newPoints = PosePointCollection.Create(_intObj.rigidbody.position,
                                                 _intObj.rigidbody.rotation);
      _controllerToPoints[controller] = newPoints;

      for (int i = 0; i < controller.graspManipulatorPoints.Count; i++) {
        Vector3 manipulatorPosition = controller.graspManipulatorPoints[i];

        newPoints.SetWorldPosition(i, manipulatorPosition);
      }
    }

    public void RemoveController(InteractionController controller) {
      var collection = _controllerToPoints[controller];
      _controllerToPoints.Remove(controller);

      // Return the collection to the pool so it can be re-used.
      PosePointCollection.Return(collection);
    }

    public void ClearControllers() {
      foreach (var controllerPointsPair in _controllerToPoints) {
        PosePointCollection.Return(controllerPointsPair.Value);
      }
      _controllerToPoints.Clear();
    }

    public void GetGraspedPosition(out Vector3 newPosition, out Quaternion newRotation) {
      _points.Clear(); _refPoints.Clear();
      Vector3    bodyPosition = _intObj.rigidbody.position;
      Quaternion bodyRotation = _intObj.rigidbody.rotation;
      Matrix4x4 it = Matrix4x4.TRS(bodyPosition, bodyRotation, Vector3.one);

      _controllerCentroid = Vector3.zero; _objectCentroid = Vector3.zero; _manipulatorCount = 0f;

      foreach (var controllerPointPair in _controllerToPoints) {
        InteractionController controller = controllerPointPair.Key;
        PosePointCollection points = _controllerToPoints[controller];

        for (int i = 0; i < controller.graspManipulatorPoints.Count; i++) {

          Vector3 originalManipulatorPos = points.GetLocalPosition(i);
          Vector3 currentManipulatorPos = controller.graspManipulatorPoints[i];

          // Perform the solve such that the objects' positions are matched to the new
          // manipulator positions.
          Vector3 point1 = (it.MultiplyPoint3x4(originalManipulatorPos) - bodyPosition);
          Vector3 point2 = (currentManipulatorPos - bodyPosition);

          if (_intObj.isPositionLocked) {
            // Only rotate the object, pivoting around its origin.
            _solveMethod = SolveMethod.PivotAroundOrigin;
            _objectCentroid += point1;
            _controllerCentroid += point2;
            _manipulatorCount += 1F;
          }
          else {
            // Do normal Kabsch solve.
            _solveMethod = SolveMethod.SixDegreeSolve;
            _points.Add(point1); _refPoints.Add(point2);
          }
        }
      }

      Matrix4x4 kabschTransform = PerformSolve(bodyPosition);

      newPosition = bodyPosition + kabschTransform.GetVector3();
      newRotation = kabschTransform.GetQuaternion() * bodyRotation;
    }

    private Matrix4x4 PerformSolve(Vector3 position) {
      switch (_solveMethod) {
        case SolveMethod.SixDegreeSolve:
          return _kabsch.SolveKabsch(_points, _refPoints);

        case SolveMethod.PivotAroundOrigin:
          _objectCentroid /= _manipulatorCount;
          _controllerCentroid /= _manipulatorCount;
          if (!_objectCentroid.Equals(_controllerCentroid)) {
            return Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(_objectCentroid, _controllerCentroid), Vector3.one);
          }
          else {
            return Matrix4x4.identity;
          }
        default:
          return _kabsch.SolveKabsch(_points, _refPoints);
      }
    }

    protected class PosePointCollection {
      // Without a pool, you might end up with 2 instances per object
      // With a pool, likely there will only ever be 2 instances!
      private static Stack<PosePointCollection> _posePointCollectionPool = new Stack<PosePointCollection>();

      private List<Vector3> _localPositions;
      private Matrix4x4 _inverseTransformMatrix;

      public static PosePointCollection Create(Vector3 position,
                                               Quaternion rotation) {
        PosePointCollection collection;
        if (_posePointCollectionPool.Count != 0) {
          collection = _posePointCollectionPool.Pop();
        } else {
          collection = new PosePointCollection();
        }

        collection.Initialize(position, rotation);
        return collection;
      }

      public static void Return(PosePointCollection posePointCollection) {
        _posePointCollectionPool.Push(posePointCollection);
      }

      private PosePointCollection() {
        _localPositions = new List<Vector3>();
      }

      private void Initialize(Vector3 position, Quaternion rotation) {
        _inverseTransformMatrix = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
      }

      public void SetWorldPosition(int index, Vector3 worldPosition) {
        Vector3 localPosition = _inverseTransformMatrix.MultiplyPoint3x4(worldPosition);

        if (index > _localPositions.Count) {
          Debug.LogError("SetWorldPosition requires setting indices in order from 0.");
        }

        if (_localPositions.Count == index) {
          _localPositions.Add(localPosition);
        }
        else {
          _localPositions[index] = localPosition;
        }
      }

      public Vector3 GetLocalPosition(int index) {
        return _localPositions[index];
      }
    }

  }

}
