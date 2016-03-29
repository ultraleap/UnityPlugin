using System;

namespace Leap.Unity.Interaction {

  public class NoControllerSpecifiedException : Exception {
    public NoControllerSpecifiedException() :
      base("There was no InteractionController specified for the InteractionBehaviour.") { }
  }

}
