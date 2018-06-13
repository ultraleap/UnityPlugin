namespace Leap.Unity.Gestures {

  public interface IGesture {

    bool isActive { get; }

    bool wasActivated { get; }

    bool wasDeactivated { get; }

    bool wasFinished { get; }

    bool wasCancelled { get; }

    bool isEligible { get; }

  }

}
