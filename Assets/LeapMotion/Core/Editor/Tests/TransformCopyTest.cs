/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
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

    [Test]
    public void AreBinaryEqual() {
      assertObjectsEqual("Frame", _originalFrame, _frame);
    }

    private void assertObjectsEqual(string objectName, object a, object b) {
      if ((a == null) != (b == null)) {
        Assert.Fail("For " + objectName + ", one object was null an the other was not.");
        return;
      }

      Type typeA = a.GetType();
      Type typeB = b.GetType();

      if (typeA != typeB) {
        Assert.Fail("For " + objectName + ", object Type " + typeA + " is not equal to type " + typeB + ".");
      }

      if (typeA.IsValueType) {
        Assert.That(a, Is.EqualTo(b), objectName);
        return;
      }

      if (a is IList) {
        IList aList = a as IList;
        IList bList = b as IList;

        Assert.That(aList.Count, Is.EqualTo(bList.Count), objectName + ".Count");

        for (int i = 0; i < aList.Count; i++) {
          assertObjectsEqual(objectName + "[" + i + "]", aList[i], bList[i]);
        }
      } else {
        FieldInfo[] fields = typeA.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
          assertObjectsEqual(objectName + "." + field.Name, field.GetValue(a), field.GetValue(b));
        }

        PropertyInfo[] properties = typeA.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties) {
          if (property.GetIndexParameters().Length == 0) {
            object propA;
            try {
              propA = property.GetValue(a, null);
            } catch (Exception exceptionA) {
              try {
                property.GetValue(b, null);
                Assert.Fail("For " + objectName + ", one property threw an exception where the other did not.");
                return;
              } catch (Exception exceptionB) {
                Assert.That(exceptionA.GetType(), Is.EqualTo(exceptionB.GetType()), "For " + objectName + ", both properties threw exceptions but their types were different.");
                return;
              }
            }

            object propB = property.GetValue(b, null);

            assertObjectsEqual(objectName + "." + property.Name, propA, propB);
          }
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

        assertVectorsEqual(oldHand.PalmPosition + translation, newHand.PalmPosition, "Palm Position");

        for (int j = 0; j < 5; j++) {
          Finger oldFinger = oldHand.Fingers[j];
          Finger newFinger = newHand.Fingers[j];

          assertVectorsEqual(oldFinger.TipPosition + translation, newFinger.TipPosition, oldFinger.Type.ToString());
        }
      }
    }
  }
}
