using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class PoseStreamIntersectFilter : MonoBehaviour,
                                           IStream<Pose>,
                                           IStreamReceiver<Pose>,
                                           IRuntimeGizmoComponent {

    public Collider intersectionVolume;

    [Range(1, 10)]
    public int framesRequired = 1;

    [MinValue(0f)]
    public float intersectionDistance = 0.01f;

    public bool drawDebug = false;

    public event Action OnOpen = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    private void Reset() {
      if (intersectionVolume == null) {
        intersectionVolume = GetComponentInChildren<Collider>();
      }
    }

    private int _intersectedCount = 0;

    public void Close() {
      OnClose();
    }

    public void Open() {
      _intersectedCount = 0;

      OnOpen();
    }

    public void Receive(Pose data) {
      bool doesIntersect;
      if (intersectionVolume == null) {
        doesIntersect = true;
      }
      else {
        doesIntersect = (data.position - intersectionVolume.ClosestPoint(data.position))
                          .sqrMagnitude < intersectionDistance * intersectionDistance;
      }

      if (doesIntersect) {
        _intersectedCount = Mathf.Min(_intersectedCount + 1, framesRequired);
      }
      else {
        _intersectedCount = 0;
      }

      if (_intersectedCount == framesRequired) {
        OnSend(data);
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (drawDebug) {
        drawer.color = LeapColor.cerulean.WithAlpha(0.5f);

        if (intersectionVolume != null) {
          drawer.DrawCollider(intersectionVolume, useWireframe: false);
        }
      }
    }

  }

}
