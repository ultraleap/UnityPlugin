using System;

namespace Leap.Unity.Interaction {

  public class HandAlreadyGraspingException : Exception {
    public HandAlreadyGraspingException(string methodName, int handId) :
      base("Cannot call " + methodName + " because there is already a hand of id " + handId + 
           " grasping this InteractionObject.") { }
  }

}
