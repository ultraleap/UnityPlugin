using System;

namespace Leap.Unity.Interaction {

  public class NotRegisteredWithControllerException : Exception {
    public NotRegisteredWithControllerException() :
      base("The object has not been registered with the InteractionController. " +
           "Try calling EnableInteraction first.") { }
  }

}
