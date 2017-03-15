using Leap.Unity.Query;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  [RequireComponent(typeof(InteractionBehaviour))]
  public class InteractiveFace : MonoBehaviour {

    public InteractiveMesh intMesh;

    public Face face;

    public static InteractiveFace Create(InteractiveMesh forMesh, Face face, GameObject basePrefab = null) {
      GameObject intFaceObj;
      if (basePrefab == null) {
        intFaceObj = new GameObject("Interactive Face");
      }
      else {
        intFaceObj = Instantiate<GameObject>(basePrefab);
      }

      intFaceObj.transform.parent = forMesh.transform;
      intFaceObj.transform.localPosition = GetLocalFaceCenter(face);

      var intFace = intFaceObj.GetComponent<InteractiveFace>() ?? intFaceObj.AddComponent<InteractiveFace>();
      intFace.intMesh = forMesh;
      intFace.face = face;
      return intFace;
    }

    private static Vector3 GetLocalFaceCenter(Face face) {
      int count = 0;
      Vector3 pos = Vector3.zero;
      foreach (var v in face.vertices) { pos += v.position; count++; }
      return pos / count;
    }

    private InteractionBehaviour _intObj;

    void Start() {
      _intObj = GetComponent<InteractionBehaviour>();
      _intObj.OnGraspBegin += OnGraspBegin;
      _intObj.OnGraspHold  += OnGraspHold;
      _intObj.OnGraspEnd   += OnGraspEnd;
    }

    public void RefreshLocation() {
      this.transform.localPosition = GetLocalFaceCenter(face);
    }

    private Transform _originalParent;
    private List<InteractiveVertex> _intVertsGrabbed = new List<InteractiveVertex>();

    private void OnGraspBegin(Hand hand) {
      _intVertsGrabbed.Clear();
      List<InteractiveVertex> tempIntVertsGrabbed = null;
      foreach (var v in face.vertices) {
        tempIntVertsGrabbed = intMesh.GetInteractiveVertices(face);
        break;
      }
      foreach (var intV in tempIntVertsGrabbed) {
        _intVertsGrabbed.Add(intV);
        _originalParent = intV.transform.parent;
        intV.transform.parent = this.transform;
      }
    }

    private void OnGraspHold(Hand hand) {
      foreach (var neighborIntFace in intMesh.GetNeighboringFaces(this.face).Query().Select((face) => { return intMesh.GetInteractiveFace(face); })) {
        neighborIntFace.RefreshLocation();
      }
    }

    private void OnGraspEnd(Hand hand) {
      foreach (var intV in _intVertsGrabbed) {
        intV.transform.parent = _originalParent;
      }
    }

  }

}
