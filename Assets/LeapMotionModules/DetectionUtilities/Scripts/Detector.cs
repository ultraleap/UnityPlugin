using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.DetectionUtilities{

public class Detector : MonoBehaviour {
    public float Period = .1f; //seconds
    public UnityEvent OnDetection;
  }
}
