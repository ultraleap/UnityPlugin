using System;

namespace Leap.Unity.Interaction {

  public class NoGraspingHandsException : Exception {
    public NoGraspingHandsException(string methodName, int handId) :
      base("Cannot call " + methodName + " because there are no hands currently grasping " +
            "this InteractionObject.") { }
  }

}
