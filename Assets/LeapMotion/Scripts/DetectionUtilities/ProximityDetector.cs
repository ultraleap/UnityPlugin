using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;

namespace Leap.Unity{

  //Detects when the parent Gameobject is in proximity of one of a list of target objects.
  public class ProximityDetector : Detector {
    [Tooltip("The interval in seconds at which to check this detector's conditions.")]
    public float Period = .1f; //seconds
    public ProximityEvent OnProximity;
    public GameObject[] TargetObjects;
    public float OnDistance = .01f; //meters
    public float OffDistance = .015f; //meters

    private IEnumerator proximityWatcherCoroutine;
    private GameObject _currentObj = null;
    public GameObject CurrentObject { get { return _currentObj; } }

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
      float onSquared, offSquared; //Use squared distamces to avoid taking square roots
      while(true){
        onSquared = OnDistance * OnDistance;
        offSquared = OffDistance * OffDistance;
        if(_currentObj != null){
          if(distanceSquared(_currentObj) > offSquared){
            _currentObj = null;
            proximityState = false;
          }
        } else {
          for(int obj = 0; obj < TargetObjects.Length; obj++){
            GameObject target = TargetObjects[obj];
            if(distanceSquared(target) < onSquared){
              _currentObj = target;
              proximityState = true;
              OnProximity.Invoke(_currentObj);
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