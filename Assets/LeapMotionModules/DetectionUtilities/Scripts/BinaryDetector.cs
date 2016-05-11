using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {

  public class BinaryDetector : Detector {
    public bool IsActive = false;
    
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
}
