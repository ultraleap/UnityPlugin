using Leap.Unity.RuntimeGizmos;
using Leap.Unity.UI.Interaction;
using Leap.Unity.UI.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint {

  [RequireComponent(typeof(InteractionBehaviour))]
  public class BuildingPrimitive : MonoBehaviour, IRuntimeGizmoComponent {

    public AnchorableBehaviour anchorControl;

    public enum ScalingMode { IndependentXYZ, YAndXZ, IdenticalXYZ }

    [Header("Scaling Mode")]
    public ScalingMode _scalingMode = ScalingMode.IndependentXYZ;

    private InteractionBehaviour _intObj;

    void Start() {
      _intObj = GetComponent<InteractionBehaviour>();

      InitGraspedControl();
    }

    #region Grasped Control

    private void InitGraspedControl() {
      // Grabbing from toolbelt, placing, returning to toolbelt
      _intObj.OnObjectGraspBegin += OnObjectGraspBegin;
      _intObj.OnObjectGraspEnd   += OnObjectGraspEnd;

      // More precise grasped movement at a cost of movement latency
      _intObj.OnGraspedMovement  += SmoothGraspedMovement;

      // "Scaling" mode when two-handed grasping
      _intObj.OnMultiGraspBegin += OnMultiGraspBegin;
      _intObj.OnMultiGraspHold  += OnMultiGraspHold;
      _intObj.OnMultiGraspEnd   += OnMultiGraspEnd;
    }

    private void OnObjectGraspBegin() {
      anchorControl.enabled = false;
      this.transform.parent = null;
    }

    private void OnObjectGraspEnd() {
      Anchor anchor;
      if (anchorControl.TryToAnchor(out anchor)) {
        this.transform.parent = anchor.transform;
      }
      //float distanceToAnchor = Vector3.Distance(this.transform.position, anchor.transform.position);
      //if (distanceToAnchor < 0.3F && toolbelt.isOpen) {
      //  _state = BuildingPrimitiveState.Anchored;
      //  this.transform.parent = anchor.transform;
      //}
      //else {
      //  _state = BuildingPrimitiveState.Placed;
      //  this.transform.parent = null;
      //}
    }

    private void SmoothGraspedMovement(Vector3 preSolvedPos, Quaternion preSolvedRot, Vector3 solvedPos, Quaternion solvedRot, Hand _) {
      // This speed-based (s)lerping allows more precise placement of objects
      // when the desired adjustment of the object's position/rotation is small.

      float lerpCoeffPerSec = Vector3.Distance(preSolvedPos, solvedPos).Map(0F, 0.2F, 0.01F, 100F);
      float slerpCoeffPerSec = Quaternion.Angle(preSolvedRot, solvedRot).Map(0F, 50F, 0.01F, 100F);

      _intObj.Rigidbody.position = Vector3.Lerp(preSolvedPos, solvedPos, lerpCoeffPerSec * Time.deltaTime);
      _intObj.Rigidbody.rotation = Quaternion.Slerp(preSolvedRot, solvedRot, slerpCoeffPerSec * Time.deltaTime);
    }

    #region Two-handed Grasping - Object Scaling

    private enum MultiGraspMode {
      Moving,
      MovingAndScaling
    }
    private MultiGraspMode _multiGraspMode = MultiGraspMode.Moving;
    private bool _scalingX, _scalingY, _scalingZ;

    private Vector3[] _objSpaceHandOffsets = new Vector3[2];
    private Vector3[] _objSpacePullVectors = new Vector3[2];

    private Vector3 GetObjectSpaceGrabPoint(Hand hand) {
      return this.transform.InverseTransformVector(_intObj.GetGraspPoint(hand) - this.transform.position);
    }

    private void OnMultiGraspBegin(List<Hand> hands) {
      // We'll only pay attention to the first two hands in the list. That's fine, this is a single-player-per-leap game.
      for (int i = 0; i < 2; i++) {
        _objSpaceHandOffsets[i] = GetObjectSpaceGrabPoint(hands[i]);
      }
    }

    private void OnMultiGraspHold(List<Hand> hands) {
      for (int i = 0; i < 2; i++) {
        _objSpacePullVectors[i] = GetObjectSpaceGrabPoint(hands[i]);
      }

      switch (_multiGraspMode) {
        case MultiGraspMode.Moving:
          Vector3 avgPullVector = (Abs(_objSpacePullVectors[0] - _objSpaceHandOffsets[0]) + Abs(_objSpacePullVectors[1] - _objSpaceHandOffsets[1])) / 2F;
          float beginScalingDisplacement = 0.02F;
          bool beginScaling = false;

          Vector3 avgPullWorldScale = Vector3.Scale(avgPullVector, this.transform.localScale);
          if (avgPullWorldScale.x > beginScalingDisplacement) {
            _scalingX = true;
            beginScaling = true;
          }
          else if (avgPullWorldScale.y > beginScalingDisplacement) {
            _scalingY = true;
            beginScaling = true;
          }
          else if (avgPullWorldScale.z > beginScalingDisplacement) {
            _scalingZ = true;
            beginScaling = true;
          }

          if (beginScaling) {
            _multiGraspMode = MultiGraspMode.MovingAndScaling;
          }
          break;
        case MultiGraspMode.MovingAndScaling: default:
          Vector3 handOffsetPull = _objSpaceHandOffsets[0] - _objSpaceHandOffsets[1];
          Vector3 scalePull      = _objSpacePullVectors[0] - _objSpacePullVectors[1];

          Vector3 scaleFraction = Vector3.Max(scalePull.CompDiv(handOffsetPull), Vector3.zero);

          Vector3 largestDeltaEffLocalScale = this.transform.localScale;

          if (!_scalingX) {
            scaleFraction = new Vector3(1F, scaleFraction.y, scaleFraction.z);
            largestDeltaEffLocalScale = new Vector3(1F, largestDeltaEffLocalScale.y, largestDeltaEffLocalScale.z);
          } 
          if (!_scalingY) {
            scaleFraction = new Vector3(scaleFraction.x, 1F, scaleFraction.z);
            largestDeltaEffLocalScale = new Vector3(largestDeltaEffLocalScale.x, 1F, largestDeltaEffLocalScale.z);
          }
          if (!_scalingZ) {
            scaleFraction = new Vector3(scaleFraction.x, scaleFraction.y, 1F);
            largestDeltaEffLocalScale = new Vector3(largestDeltaEffLocalScale.x, largestDeltaEffLocalScale.y, 1F);
          }

          switch (_scalingMode) {
            case ScalingMode.IndependentXYZ:
              // do nothing; this mode is the default behavior
              break;
            case ScalingMode.YAndXZ:
              // Y scales independently, but XZ are locked together.
              float scaleFractionXZ = _scalingX ? scaleFraction.x : scaleFraction.z;
              scaleFraction = new Vector3(scaleFractionXZ, scaleFraction.y, scaleFractionXZ);
              break;
            case ScalingMode.IdenticalXYZ:
              if (_scalingX) {
                scaleFraction = new Vector3(scaleFraction.x, scaleFraction.x, scaleFraction.x);
              }
              else if (_scalingY) {
                scaleFraction = new Vector3(scaleFraction.y, scaleFraction.y, scaleFraction.y);
              }
              else {
                scaleFraction = new Vector3(scaleFraction.z, scaleFraction.z, scaleFraction.z);
              }
              break;
          }

          Vector3 scaleTarget = Vector3.Scale(this.transform.localScale, scaleFraction);

          float largestDelta = LargestAbsDeltaComponent(scaleTarget, largestDeltaEffLocalScale);

          float lerpCoeffPerSec = largestDelta.Map(0.001F, 1F, 2F, 50F);
          this.transform.localScale = Vector3.Lerp(this.transform.localScale, scaleTarget, lerpCoeffPerSec * Time.deltaTime);

          break;
      }
    }

    private void OnMultiGraspEnd(List<Hand> hands) {
      for (int i = 0; i < 2; i++) {
        _objSpacePullVectors[i] = Vector3.zero;
      }

      _multiGraspMode = MultiGraspMode.Moving;
      _scalingX = false;
      _scalingY = false;
      _scalingZ = false;
    }

    private bool _drawGizmos = false;
    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!_drawGizmos) return;
      Vector3 basePos = this.transform.position + Vector3.up * 0.1F;

      drawer.color = Color.red;
      drawer.DrawSphere(basePos, 0.001F);

      drawer.color = Color.red;
      drawer.DrawLine(basePos, basePos + Vector3.right * _objSpacePullVectors[0].x * 0.01F);
      drawer.DrawLine(basePos, basePos + Vector3.right * _objSpacePullVectors[1].x * 0.01F);
      drawer.color = Color.green;
      drawer.DrawLine(basePos, basePos + Vector3.up * _objSpacePullVectors[0].y * 0.01F);
      drawer.DrawLine(basePos, basePos + Vector3.up * _objSpacePullVectors[1].y * 0.01F);
      drawer.color = Color.blue;
      drawer.DrawLine(basePos, basePos + Vector3.forward * _objSpacePullVectors[0].z * 0.01F);
      drawer.DrawLine(basePos, basePos + Vector3.forward * _objSpacePullVectors[1].z * 0.01F);

      drawer.color = Color.red;
      drawer.DrawSphere(this.transform.TransformPoint(_objSpaceHandOffsets[0]), 0.011F);
      drawer.DrawSphere(this.transform.TransformPoint(_objSpaceHandOffsets[1]), 0.011F);
      drawer.color = Color.black;
      drawer.DrawSphere(this.transform.TransformPoint(_objSpacePullVectors[0]), 0.013F);
      drawer.DrawSphere(this.transform.TransformPoint(_objSpacePullVectors[1]), 0.013F);
    }

    #endregion

    #endregion

    private static Vector3 Abs(Vector3 v) {
      return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private static float LargestAbsDeltaComponent(Vector3 A, Vector3 B) {
      float xD = Mathf.Abs(A.x - B.x);
      float yD = Mathf.Abs(A.y - B.y);
      float zD = Mathf.Abs(A.z - B.z);
      float largest = xD;
      if (xD > largest) largest = yD;
      if (zD > largest) largest = zD;
      return largest;
    }

  }

}