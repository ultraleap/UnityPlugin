using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  [ExecuteInEditMode]
  public class WorkstationPoseTest : MonoBehaviour {

    public Transform userCamera;
    public Transform stationObj;
    public Transform stationObjOneSecLater;

    public float myRadius;

    public Transform otherOpenStationsParent;

    [Disable]
    public List<Vector3> otherOpenStationPositions = new List<Vector3>();
    [Disable]
    public List<float> otherOpenStationRadii = new List<float>();

    void Update() {
      if (userCamera == null) return;
      if (stationObj == null) return;
      if (stationObjOneSecLater == null) return;
      if (otherOpenStationsParent == null) return;

      refreshLists();
      refreshRadius();

      Vector3 targetPosition = WorkstationBehaviour.DefaultDetermineWorkstationPosition(userCamera.position, userCamera.rotation,
                                                               stationObj.position, (stationObjOneSecLater.position - stationObj.position),
                                                               otherOpenStationPositions, otherOpenStationRadii);

      Quaternion targetRotation = WorkstationBehaviour.DefaultDetermineWorkstationRotation(userCamera.position, targetPosition);

      this.transform.position = targetPosition;
      this.transform.rotation = targetRotation;
    }

    private void refreshLists() {
      otherOpenStationPositions.Clear();
      otherOpenStationRadii.Clear();

      if (otherOpenStationsParent != null) {
        foreach (var child in otherOpenStationsParent.GetChildren()) {
          var radiusProvider = child.GetComponent<RenderWireSphere>();
          if (radiusProvider != null) {
            otherOpenStationPositions.Add(radiusProvider.transform.position);
            otherOpenStationRadii.Add(radiusProvider.radius);
          }
        }
      }
    }

    private void refreshRadius() {
      var radiusProvider = GetComponent<RenderWireSphere>();
      if (radiusProvider == null) return;

      myRadius = radiusProvider.radius;
    }
  }

}
