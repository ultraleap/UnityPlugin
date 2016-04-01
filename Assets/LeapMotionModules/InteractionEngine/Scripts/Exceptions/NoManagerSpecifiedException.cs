using System;

namespace Leap.Unity.Interaction {

  public class NoManagerSpecifiedException : Exception {
    public NoManagerSpecifiedException() :
      base("There was no InteractionManager specified for the InteractionBehaviour.") { }
  }

}
