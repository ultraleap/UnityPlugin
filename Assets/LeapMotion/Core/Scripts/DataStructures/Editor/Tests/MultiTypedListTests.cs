/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using NUnit.Framework;

namespace Leap.Unity.Tests {

  public class MultiTypedListTests {

    public class BaseClass { }
    public class A : BaseClass { }
    public class B : BaseClass { }
    public class C : BaseClass { }
    public class D : BaseClass { }
    public class E : BaseClass { }
    public class F : BaseClass { }
    public class G : BaseClass { }
    public class H : BaseClass { }

    public class InvalidClass : BaseClass { }

    private class ListClass : MultiTypedList<BaseClass, A, B, C, D, E, F, G, H> { }

    private ListClass _list;

    [SetUp]
    public void Setup() {
      _list = new ListClass();
    }

    [TearDown]
    public void Teardown() {
      _list = null;
    }

    [Test]
    public void InsertTests() {
      addOneOfEach();

      int countBefore = _list.Count;

      _list.Insert(0, new A());

      int countAfter = _list.Count;

      Assert.That(countAfter, Is.EqualTo(countBefore + 1));

      Assert.That(_list[0], Is.TypeOf<A>());
      Assert.That(_list[1], Is.TypeOf<A>());
      Assert.That(_list[2], Is.TypeOf<B>());
    }

    [Test]
    public void EnumerableTest() {
      addOneOfEach();

      List<BaseClass> objs = new List<BaseClass>();
      foreach (var obj in _list) {
        objs.Add(obj);
      }

      Assert.That(objs[0], Is.TypeOf<A>());
      Assert.That(objs[1], Is.TypeOf<B>());
      Assert.That(objs[2], Is.TypeOf<C>());
      Assert.That(objs[3], Is.TypeOf<D>());
      Assert.That(objs[4], Is.TypeOf<E>());
      Assert.That(objs[5], Is.TypeOf<F>());
      Assert.That(objs[6], Is.TypeOf<G>());
      Assert.That(objs[7], Is.TypeOf<H>());
    }

    [Test]
    public void RemoveAtTest1() {
      addOneOfEach();
      Assert.That(_list.Count, Is.EqualTo(8));

      for (int i = 0; i < 8; i++) {
        _list.RemoveAt(0);
      }

      Assert.That(_list.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemoveAtTest2() {
      addOneOfEach();
      Assert.That(_list.Count, Is.EqualTo(8));

      for (int i = 8; i-- != 0;) {
        _list.RemoveAt(i);
      }

      Assert.That(_list.Count, Is.EqualTo(0));
    }

    [Test]
    public void IndexTest1() {
      addOneOfEach();

      Assert.That(_list[0], Is.TypeOf<A>());
      Assert.That(_list[1], Is.TypeOf<B>());
      Assert.That(_list[2], Is.TypeOf<C>());
      Assert.That(_list[3], Is.TypeOf<D>());
      Assert.That(_list[4], Is.TypeOf<E>());
      Assert.That(_list[5], Is.TypeOf<F>());
      Assert.That(_list[6], Is.TypeOf<G>());
      Assert.That(_list[7], Is.TypeOf<H>());
    }

    [Test]
    public void IndexTest2() {
      addOneOfEach();

      _list.RemoveAt(0);
      _list.RemoveAt(0);
      _list.RemoveAt(0);
      _list.RemoveAt(0);

      Assert.That(_list[0], Is.TypeOf<E>());
      Assert.That(_list[1], Is.TypeOf<F>());
      Assert.That(_list[2], Is.TypeOf<G>());
      Assert.That(_list[3], Is.TypeOf<H>());
    }

    [Test]
    public void AssignTest() {
      addOneOfEach();

      A a = new A();
      _list[5] = a;

      Assert.That(_list[0], Is.TypeOf<A>());
      Assert.That(_list[1], Is.TypeOf<B>());
      Assert.That(_list[2], Is.TypeOf<C>());
      Assert.That(_list[3], Is.TypeOf<D>());
      Assert.That(_list[4], Is.TypeOf<E>());
      Assert.That(_list[5], Is.EqualTo(a));
      Assert.That(_list[6], Is.TypeOf<G>());
      Assert.That(_list[7], Is.TypeOf<H>());
    }

    [Test]
    public void AddInvalidObjectTest() {
      Assert.That(() => {
        _list.Add(new InvalidClass());
      }, Throws.ArgumentException);
    }

    private void addOneOfEach() {
      _list.Add(new A());
      _list.Add(new B());
      _list.Add(new C());
      _list.Add(new D());
      _list.Add(new E());
      _list.Add(new F());
      _list.Add(new G());
      _list.Add(new H());
    }

  }
}
