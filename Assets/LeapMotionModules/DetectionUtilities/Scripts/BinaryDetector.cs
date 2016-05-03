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
      IsActive = true;
      OnActivate.Invoke();
      OnDetection.Invoke();
    }
  
    public virtual void Deactivate(){
      IsActive = false;
      OnDeactivate.Invoke();
      OnDetection.Invoke();
    }
  }
}
