
namespace Leap.Unity.Recording {

  public class LeapPlayableProvider : LeapProvider {

    private Frame _frame;

    public override Frame CurrentFixedFrame {
      get {
        return _frame;
      }
    }

    public override Frame CurrentFrame {
      get {
        return _frame;
      }
    }

    public void SetCurrentFrame(Frame frame) {
      _frame = frame;
    }
  }
}
