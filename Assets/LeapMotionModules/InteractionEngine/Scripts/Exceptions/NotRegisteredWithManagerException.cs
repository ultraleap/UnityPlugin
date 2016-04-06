using System;

namespace Leap.Unity.Interaction {

  public class NotRegisteredWithManagerException : Exception {
    public NotRegisteredWithManagerException() :
      base("The object has not been registered with the InteractionManager. " +
           "Try calling EnableInteraction first.") { }
  }

}
