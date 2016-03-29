using System;

namespace Leap.Unity.Interaction {

  public class NoGraspingHandsException : Exception {
    public NoGraspingHandsException(int handId) :
      base("There are no hands currently grasping " + handId + "this InteractionObject.") { }
  }

}
