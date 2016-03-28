using System;
using Leap;

namespace InteractionEngine {

  public class HandNotGraspingException : Exception {
    public HandNotGraspingException(string methodName, int handId) :
      base("Cannot call " + methodName + " because there is not a hand with id " +
            "currently grasping this InteractionObject.") { }
  }

}
