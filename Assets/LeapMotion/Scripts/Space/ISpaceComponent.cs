using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Space {

  public interface ISpaceComponent {
    LeapSpaceAnchor anchor { get; }
  }
}
