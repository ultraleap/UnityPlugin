using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Internal;

namespace Leap.Unity {
  public class MultideviceAlignment : MonoBehaviour {
    [Serializable]
    public struct MultideviceCalibrationInfo {
      public LeapProvider deviceProvider;
      [NonSerialized]
      public List<Vector3> handPoints;
      [NonSerialized]
      public Hand currentHand;
    }
    public MultideviceCalibrationInfo[] devices;

    public KeyCode takeCalibrationSampleKey;
    public KeyCode solveForRelativeTransformKey;

    // Use this for initialization
    void Start() {}

    // Update is called once per frame
    void Update() {
      if (devices.Length > 1) {

        // Add the set of joints to device-specific lists
        if (Input.GetKeyUp(takeCalibrationSampleKey)) {
          bool handInAllFrames = true;
          for (int i = 0; i < devices.Length; i++) {
            Hand rightHand = devices[i].deviceProvider.CurrentFrame.Get(Chirality.Right);
            if (rightHand != null) {
              devices[i].currentHand = rightHand;
            } else {
              handInAllFrames = false;
            }
          }

          if (handInAllFrames) {
            for (int i = 0; i < devices.Length; i++) {
              if (devices[i].handPoints == null) devices[i].handPoints = new List<Vector3>();
              for(int j = 0; j < 5; j++) {
                for (int k = 0; k < 4; k++) {
                  devices[i].handPoints.Add(devices[i].currentHand.Fingers[j].bones[k].Center.ToVector3());
                }
              }
            }
          }
        }

        // Moves subsidiary devices to be in alignment with the device at the 0 index
        if (Input.GetKeyUp(solveForRelativeTransformKey)) {
          if (devices[0].handPoints.Count > 3) {
            KabschSolver solver = new KabschSolver();

            for (int i = 1; i < devices.Length; i++) {

              List<Vector3> referenceValues = new List<Vector3>(devices[0].handPoints);

              Matrix4x4 deviceToOriginDeviceMatrix = 
                solver.SolveKabsch(devices[i].handPoints, referenceValues, 200);

              devices[i].deviceProvider.transform.Transform(deviceToOriginDeviceMatrix);

              devices[i].handPoints.Clear();
            }
            devices[0].handPoints.Clear();
          }
        }
      }

    }

    private void OnDrawGizmos() {
      for (int i = 0; i < devices.Length; i++) {
        if (devices[0].handPoints != null) {
          for (int j = 0; j < devices[0].handPoints.Count; j++) {
            Gizmos.DrawSphere(devices[i].handPoints[j], 0.01f);
            if (i > 0) {
              Gizmos.DrawLine(devices[i].handPoints[j], devices[0].handPoints[j]);
            }
          }
        }
      }
    }

  }
}