using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Leap.Unity {

  public class DetectorLogicGate : Detector {
    public List<Detector> Detectors;
    public bool AddAllDetectorsOnAwake = true;
    public LogicType GateType = LogicType.AndGate;
    public bool Negate = false;

    public void AddDetector(Detector detector){
      if(!Detectors.Contains(detector)){
        Detectors.Add(detector);
      }
    }

    public void RemoveDetector(Detector detector){
      detector.OnActivate.RemoveListener(CheckDetectors);
      detector.OnDeactivate.RemoveListener(CheckDetectors);
      Detectors.Remove(detector);
    }

    public void AddAllDetectors(){
      Detector[] detectors = GetComponents<Detector>();
      for(int g = 0; g < detectors.Length; g++){
        if ( detectors[g].GetInstanceID() != this.GetInstanceID() && detectors[g].enabled) {
          AddDetector(detectors[g]);
        }
      }
    }

    private void Awake(){
      if(AddAllDetectorsOnAwake){
        AddAllDetectors();
      }
      foreach(Detector detector in Detectors){
        activateDetector(detector);
      }
    }

    private void activateDetector(Detector detector){
      detector.OnActivate.RemoveListener(CheckDetectors); //avoid double subscription
      detector.OnDeactivate.RemoveListener(CheckDetectors);
      detector.OnActivate.AddListener(CheckDetectors);
      detector.OnDeactivate.AddListener(CheckDetectors);
    }

    private void OnDisable () {
      Deactivate();
    }

    protected void CheckDetectors(){
      if (Detectors.Count < 1)
        return;
      bool state = Detectors[0].IsActive;
      for(int a = 1; a < Detectors.Count; a++){
        if(GateType == LogicType.AndGate){
          state = state && Detectors[a].IsActive;
        } else {
          state = state || Detectors[a].IsActive;
        }
      }

      if(Negate){
        state = !state;
      }

      if(state){
        Activate();
      } else {
        Deactivate();
      }
    }
  }

  public enum LogicType{ AndGate, OrGate }
}