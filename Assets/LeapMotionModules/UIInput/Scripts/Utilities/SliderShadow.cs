using UnityEngine;
using System.Collections;

namespace Leap.Unity.InputModule {
  public class SliderShadow : MonoBehaviour {
    public Transform Slider;
    public Transform Handle;

    void Update() {
      Vector3 LocalHandle = Slider.InverseTransformPoint(Handle.position);
      Vector3 LocalShadow = Slider.InverseTransformPoint(transform.position);
      Vector3 NewLocalShadow = new Vector3(LocalHandle.x, LocalHandle.y, LocalShadow.z);
      transform.position = Slider.TransformPoint(NewLocalShadow);
    }
  }
}