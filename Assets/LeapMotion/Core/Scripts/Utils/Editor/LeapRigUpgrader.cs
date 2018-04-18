using UnityEditor;
using UnityEngine;

namespace Leap.Unity {

  public static class LeapRigUpgrader {
    
    [LeapProjectCheck("Core", 0)]
    private static bool checkSceneAndDrawGUI() {
      return true;
    }

  }

}
