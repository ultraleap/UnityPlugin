using System;
using UnityEngine;

namespace Leap.Unity.Interaction.Testing {

  public class TestProvider : LeapProvider {

    public override Frame CurrentFixedFrame {
      get {
        return null;
      }
    }

    public override Frame CurrentFrame {
      get {
        return null;
      }
    }

    public override Image CurrentImage {
      get {
        return null;
      }
    }

  }
}
