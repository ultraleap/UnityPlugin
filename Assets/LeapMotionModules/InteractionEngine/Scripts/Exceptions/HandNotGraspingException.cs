using System;

namespace Leap.Unity.Interaction {

  public class HandNotGraspingException : Exception {
    public HandNotGraspingException(string methodName, int handId) :
      base("Cannot call " + methodName + " because there is not a hand with id " +
            "currently grasping this InteractionObject.") { }
  }

}
