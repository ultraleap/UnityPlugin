using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Leap.Unity {

  public class DetectorAndGate : MonoBehaviour {
    public bool IsActive;
    public UnityEvent OnActivate;
    public UnityEvent OnDeactivate;
  
    [SerializeField]
    private Detector[] _detectors;
  
    void Start(){
      _detectors = GetComponents<Detector>();
      foreach(Detector activator in _detectors){
        activator.OnActivate.AddListener(CheckDetectors);
        activator.OnDeactivate.AddListener(CheckDetectors);
      }
    }

    void CheckDetectors(){
      if (_detectors.Length < 1)
        return;
      bool state = true;
      for(int a = 0; a < _detectors.Length; a++){
        state = state && _detectors[a].IsActive;
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