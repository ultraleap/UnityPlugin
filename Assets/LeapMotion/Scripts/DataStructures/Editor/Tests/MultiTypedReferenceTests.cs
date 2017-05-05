/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using NUnit.Framework;

namespace Leap.Unity {

  public class MultiTypedReferenceTests {

    public class BaseClass { }
    public class A : BaseClass { }
    public class B : BaseClass { }
    public class C : BaseClass { }
    public class D : BaseClass { }

    public class InvalidClass : BaseClass { }

    private class ReferenceClass : MultiTypedReference<BaseClass, A, B, C, D> { }

    private ReferenceClass _ref;

    [SetUp]
    public void Setup() {
      _ref = new ReferenceClass();
    }

    [TearDown]
    public void Teardown() {
      _ref.Clear();
      _ref = null;
    }

    [Test]
    public void SetTest() {
      _ref.Value = new A();
      Assert.That(_ref.Value, Is.TypeOf<A>());
    }

    [Test]
    public void SetNullTest() {
      _ref.Value = new A();
      Assert.That(_ref.Value, Is.TypeOf<A>());
      _ref.Value = null;
      Assert.That(_ref.Value, Is.Null);
    }

    [Test]
    public void SwitchTypeTest() {
      _ref.Value = new A();
      Assert.That(_ref.Value, Is.TypeOf<A>());
      _ref.Value = new B();
      Assert.That(_ref.Value, Is.TypeOf<B>());
    }

    [Test]
    public void ClearTest() {
      _ref.Value = new A();
      Assert.That(_ref.Value, Is.TypeOf<A>());
      _ref.Clear();
      Assert.That(_ref.Value, Is.Null);
    }

    [Test]
    public void AddInvalidTest() {
      Assert.That(() => {
        _ref.Value = new InvalidClass();
      }, Throws.ArgumentException);
    }

    [Test]
    public void CanAddAllTest() {
      _ref.Value = new A();
      Assert.That(_ref.Value, Is.TypeOf<A>());
      _ref.Value = new B();
      Assert.That(_ref.Value, Is.TypeOf<B>());
      _ref.Value = new C();
      Assert.That(_ref.Value, Is.TypeOf<C>());
      _ref.Value = new D();
      Assert.That(_ref.Value, Is.TypeOf<D>());
    }
  }
}
