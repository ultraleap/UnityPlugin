using UnityEngine;
using System.Collections;

namespace Leap.Unity.InputModule {
  public interface ILeapWidget {
    void Expand();
    void Retract();
    void HoverDistance(float distance);
  }
}