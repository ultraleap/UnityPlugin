using UnityEngine.Assertions;
using System;

namespace Leap.Unity.Interaction {

  public partial class InteractionManager {

    public void Validate() {
      Assert.AreEqual(isActiveAndEnabled, _hasSceneBeenCreated,
                      "Activation status should always be equal to scene creation status.");

      Assert.AreEqual(isActiveAndEnabled, _scene.pScene != IntPtr.Zero,
                      "Scene ptr should always be non-null when manager is active.");

      assertNonNullWhenActive(_activityManager, "Activity Manager");
      assertNonNullWhenActive(_shapeDescriptionPool, "Shape Description Pool");
      assertNonNullWhenActive(_instanceHandleToBehaviour, "Instance Handle mapping");
      assertNonNullWhenActive(_idToInteractionHand, "Id To Hand mapping");
      assertNonNullWhenActive(_graspedBehaviours, "Grasped behaviour list");




    }

    private void assertNonNullWhenActive(object obj, string name) {
      Assert.AreEqual(isActiveAndEnabled, obj != null,
                      name + " should always be non-null when manager is active.");
    }

  }
}
