using System;

namespace Leap.Unity.Interaction {

  public class HandNotGraspingException : Exception {
    public HandNotGraspingException(int handId) :
      base("There is not a hand with id " + handId + " currently grasping this InteractionObject.") { }
  }

}
