using System;

namespace Leap.Unity.Interaction {

  public class HandAlreadyUntrackedException : Exception {
    public HandAlreadyUntrackedException(int handId) :
      base("There is already an untracked hand of id " + handId + " grasping this InteractionObject.") { }
  }

}
