using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;

namespace Leap.Unity {

  public class Detector : MonoBehaviour {
    [HideInInspector]
    public bool IsActive = false;
    public float Period = .1f; //seconds
    public bool ShowGizmos = true;
    
    public UnityEvent OnActivate;
    public UnityEvent OnDeactivate;
  
    public virtual void Activate(){
      if (!IsActive) {
        IsActive = true;
        OnActivate.Invoke();
      }
      IsActive = true;
    }
  
    public virtual void Deactivate(){
      if (IsActive) {
        IsActive = false;
        OnDeactivate.Invoke();
      }
      IsActive = false;
    }
  }

  public enum PointingType { RelativeToCamera, RelativeToHorizon, RelativeToWorld, AtTarget}

}
