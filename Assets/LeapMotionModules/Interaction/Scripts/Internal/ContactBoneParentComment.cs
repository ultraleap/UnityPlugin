using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class ContactBoneParentComment : Comment {

    public InteractionController controller;

    void Reset() {
      _comment = "Contact bone parents must have no parent and must not have their "
               + "transforms translated; otherwise, child Colliders will not have "
               + "their rigidbodies' velocities set correctly.";
    }

  }

}
