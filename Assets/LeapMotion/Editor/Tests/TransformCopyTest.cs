using NUnit.Framework;

namespace Leap.Unity.Tests {

  public class TransformCopyIdentity : FrameValidator {
    protected Frame _originalFrame;

    protected override Frame createFrame() {
      _originalFrame = TestHandFactory.MakeTestFrame(0, true, true);
      return _originalFrame.TransformedCopy(LeapTransform.Identity);
    }

    [Test]
    public void IdsAreSame() {
      Assert.That(_frame.Hands.Count, Is.EqualTo(_originalFrame.Hands.Count));

      for (int i = 0; i < _frame.Hands.Count; i++) {
        Hand oldHand = _originalFrame.Hands[i];
        Hand newHand = _frame.Hands[i];
        Assert.That(oldHand.Id, Is.EqualTo(newHand.Id));

        for (int j = 0; j < 5; j++) {
          Finger oldFinger = oldHand.Fingers[j];
          Finger newFinger = newHand.Fingers[j];
          Assert.That(oldFinger.Id, Is.EqualTo(newFinger.Id));
        }
      }
    }
  }

  public class TransformCopyTranslation : FrameValidator {
    protected static Vector translation = Vector.Forward;
    protected Frame _originalFrame;

    protected override Frame createFrame() {
      _originalFrame = TestHandFactory.MakeTestFrame(0, true, true);
      LeapTransform forwardTransform = new LeapTransform(translation, LeapQuaternion.Identity);
      return _originalFrame.TransformedCopy(forwardTransform);
    }

    [Test]
    public void IsTranslated() {
      for (int i = 0; i < _frame.Hands.Count; i++) {
        Hand oldHand = _originalFrame.Hands[i];
        Hand newHand = _frame.Hands[i];

        assertVectorsEqual(oldHand.PalmPosition + translation, newHand.PalmPosition);

        for (int j = 0; j < 5; j++) {
          Finger oldFinger = oldHand.Fingers[j];
          Finger newFinger = newHand.Fingers[j];

          assertVectorsEqual(oldFinger.TipPosition + translation, newFinger.TipPosition);
        }
      }
    }
  }
}
