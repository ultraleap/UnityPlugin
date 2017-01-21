using Leap.Unity;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI {

  [RequireComponent(typeof(SphereCollider))]
  public class SweepSphere : MonoBehaviour, IRuntimeGizmoComponent {

    private BoxCollider[] _boxColliders;
    private float _sphereRadius;

    void Start() {
      _boxColliders = FindObjectsOfType<BoxCollider>();
      _previousPosition = this.transform.position;

      _sphereRadius = GetComponent<SphereCollider>().radius * this.transform.lossyScale.x;
    }

    private Vector3 _previousPosition;
    void Update() {
    Vector3 curPosition = this.transform.position;
    float sweepDistance = Vector3.Distance(_previousPosition, curPosition);
    if (sweepDistance > 0.0001F) {

      Ray sweepSegment = new Ray(_previousPosition, curPosition - _previousPosition);
      BoxCollider closestBox = null;
      RaycastHit closestBoxHitInfo = default(RaycastHit);

      /* for gizmos: */
      _sweepBuffer.Add(sweepSegment);
      _sweepDistanceBuffer.Add(sweepDistance);
      /* end gizmos */

      RaycastHit hitInfo;
      foreach (var box in _boxColliders) {
        if (box.Raycast(sweepSegment, out hitInfo, sweepDistance)) {
          if (closestBox == null) {
            closestBox = box;
            closestBoxHitInfo = hitInfo;
          }
          else {
            if (hitInfo.distance < closestBoxHitInfo.distance) {
              closestBox = box;
              closestBoxHitInfo = hitInfo;
            }
          }
        }
      }

      if (closestBox != null) {

        // Gizmos
        _boxHitNormal.Add(new Ray(closestBoxHitInfo.point, closestBoxHitInfo.normal));
        // End gizmos

        float freeSweepDistance = closestBoxHitInfo.distance - (_sphereRadius * 0.2F);
        Vector3 sweepSlide = Vector3.ProjectOnPlane(sweepSegment.direction * (sweepDistance - freeSweepDistance) * 0.7F, closestBoxHitInfo.normal);
        Vector3 freeSweep = sweepSegment.direction * freeSweepDistance;

        Vector3 sweepAndSlidePosition = _previousPosition + freeSweep + sweepSlide;
        DepenetrationRay popOut;
        BoxDepenetrator.Depenetrate(sweepAndSlidePosition, _sphereRadius, closestBox, out popOut);

        this.transform.position = sweepAndSlidePosition + (-popOut.direction * 0.5F);
      }
    }

    _previousPosition = this.transform.position;
  }

    #region Gizmos

    private RingBuffer<Ray> _sweepBuffer = new RingBuffer<Ray>(10);
    private RingBuffer<float> _sweepDistanceBuffer = new RingBuffer<float>(10);

    private RingBuffer<Ray> _boxHitNormal = new RingBuffer<Ray>(10);

    void OnDrawGizmos() {
      Gizmos.color = Color.white;
      for (int i = 0; i < _sweepBuffer.Length; i++) {
        Gizmos.DrawRay(_sweepBuffer.Get(i).origin, _sweepBuffer.Get(i).direction * _sweepDistanceBuffer.Get(i));
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawer.color = Color.white;
      for (int i = 0; i < _sweepBuffer.Length; i++) {
        drawer.DrawLine(_sweepBuffer.Get(i).origin, _sweepBuffer.Get(i).origin + _sweepBuffer.Get(i).direction * _sweepDistanceBuffer.Get(i));
      }

      drawer.color = Color.red;
      for (int i = 0; i < _boxHitNormal.Length; i++) {
        drawer.DrawLine(_boxHitNormal.Get(i).origin, _boxHitNormal.Get(i).origin + _boxHitNormal.Get(i).direction * 0.1F);
      }
    }

    #endregion

  }

}
