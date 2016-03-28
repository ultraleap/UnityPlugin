using System;
using Leap;

namespace InteractionEngine {

  public class HandAlreadyGraspingException : Exception {
    public HandAlreadyGraspingException(string methodName, int handId) :
      base("Cannot call " + methodName + " because there is already a hand of id " + handId + 
           " grasping this InteractionObject.") { }
  }

}
