using UnityEngine;
using System;

namespace Leap.Unity {

  public class SceneSettings : MonoBehaviour {

    public class ToggleValue<T> {
      public bool Override;
      public T Value;
    }

    [Serializable]
    public class ToggleFloat : ToggleValue<float> { }

    [Serializable]
    public class ToggleVector3 : ToggleValue<Vector3> { }

    [SerializeField]
    private ToggleFloat _shadowDistance = new ToggleFloat();

    [SerializeField]
    private ToggleVector3 _gravity = new ToggleVector3();

    void Reset() {
      _shadowDistance.Value = QualitySettings.shadowDistance;
      _gravity.Value = Physics.gravity;
    }

    void Awake() {
      if (_shadowDistance.Override) {
        QualitySettings.shadowDistance = _shadowDistance.Value;
      }

      if (_gravity.Override) {
        Physics.gravity = _gravity.Value;
      }
    }
  }
}
