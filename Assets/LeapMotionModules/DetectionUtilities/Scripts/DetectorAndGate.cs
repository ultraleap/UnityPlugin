using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Leap.Unity.DetectionUtilities {

  public class DetectorAndGate : MonoBehaviour {
    public bool IsActive;
    public UnityEvent OnActivate;
    public UnityEvent OnDeactivate;
  
    private BinaryDetector[] _detectors;
  
    void Start(){
      _detectors = GetComponents<BinaryDetector>();
      foreach(BinaryDetector activator in _detectors){
        activator.OnActivate.AddListener(CheckDetectors);
        activator.OnDeactivate.AddListener(CheckDetectors);
      }
    }
  
    void CheckDetectors(){
      bool state = true;
      for(int a = 0; a < _detectors.Length; a++){
        state &= _detectors[a].IsActive; 
      }
      if(state && !IsActive){
        Activate();
      } else if(!state && IsActive){
        Deactivate();
      }
    }
  
    public virtual void Activate(){
      IsActive = true;
      OnActivate.Invoke();
    }
  
    public virtual void Deactivate(){
      IsActive = false;
      OnDeactivate.Invoke();
    }

    void OnDisable () {
      Deactivate();
    }
  }
}