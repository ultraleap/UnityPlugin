using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;
using Leap.Unity;
namespace Leap.Unity.DetectionUtilities{

  //A zone detector that surounds the leap Motion Detector in concentric rings
  public class ProximityDetector : BinaryDetector {
    public ProximityEvent OnProximity;
    public GameObject[] TargetObjects;
    public float OnDistance = .05f; //meters
    public float OffDistance = .075f; //meters

    private IEnumerator proximityWatcherCoroutine;
    private GameObject currentObj = null;

    void Awake(){
      proximityWatcherCoroutine = proximityWatcher();
    }

    void OnEnable () {
        StopCoroutine(proximityWatcherCoroutine);
        StartCoroutine(proximityWatcherCoroutine);
    }
  
    void OnDisable () {
      StopCoroutine(proximityWatcherCoroutine);
    }

    IEnumerator proximityWatcher(){
      bool proximityState = false;
      float onSquared, offSquared;
      Vector3 closest;
      Collider targetCollider;
      while(true){
        onSquared = OnDistance * OnDistance;
        offSquared = OffDistance * OffDistance;
        if(currentObj != null){
          if(distanceSquared(currentObj) > offSquared){
            currentObj = null;
            proximityState = false;
          }
        } else {
          for(int obj = 0; obj < TargetObjects.Length; obj++){
            GameObject target = TargetObjects[obj];
            if(distanceSquared(target) < onSquared){
              currentObj = target;
              proximityState = true;
              OnProximity.Invoke(currentObj);
              break; // pick first match
            }
          }
        }
        if(proximityState){
          Activate();
        } else {
          Deactivate();
        }
        yield return new WaitForSeconds(Period);
      }
    }

    private float distanceSquared(GameObject target){
      Collider targetCollider = target.GetComponent<Collider>();
      Vector3 closestPoint;
      if(targetCollider != null){
        closestPoint = targetCollider.ClosestPointOnBounds(transform.position);
      } else {
        closestPoint = target.transform.position;
      }
      return (closestPoint - transform.position).sqrMagnitude;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos(){
      if(IsActive){
        Gizmos.color = Color.green;
      } else {
        Gizmos.color = Color.red;
      }
      Gizmos.DrawWireSphere(transform.position, OnDistance);
      Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(transform.position, OffDistance);
    }
    #endif
  }

  [System.Serializable]
  public class ProximityEvent : UnityEvent <GameObject> {}
}