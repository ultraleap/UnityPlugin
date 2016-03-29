using System;

namespace Leap.Unity.Interaction {

  public class HandAlreadyGraspingException : Exception {
    public HandAlreadyGraspingException(int handId) :
      base("There is already a hand of id " + handId + " grasping this InteractionObject.") { }
  }

}
