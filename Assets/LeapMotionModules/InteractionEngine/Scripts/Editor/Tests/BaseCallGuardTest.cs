using UnityEngine;
using NUnit.Framework;
using System.Collections;

namespace Leap.Unity.Interaction.Tests {

  public class BaseCallGuardTest {
    private const string KEY_A = "KeyA";
    private const string KEY_B = "KeyB";

    private BaseCallGuard _guard;

    [SetUp]
    public void Setup() {
      _guard = new BaseCallGuard();
    }

    [TearDown]
    public void Teardown() {
      _guard = null;
    }

    [Test]
    public void BaseCalled() {
      _guard.Begin(KEY_A);
      _guard.NotifyBaseCalled(KEY_A);
      _guard.AssertBaseCalled();
    }

    [Test]
    public void Recursive1() {
      _guard.Begin(KEY_A);
      _guard.NotifyBaseCalled(KEY_A);
      _guard.Begin(KEY_B);
      _guard.NotifyBaseCalled(KEY_B);
      _guard.AssertBaseCalled();
      _guard.AssertBaseCalled();
    }

    [Test]
    public void Recursive2() {
      _guard.Begin(KEY_A);
      _guard.NotifyBaseCalled(KEY_A);
      _guard.Begin(KEY_B);
      _guard.NotifyBaseCalled(KEY_B);
      _guard.Begin(KEY_A);
      _guard.NotifyBaseCalled(KEY_A);
      _guard.Begin(KEY_B);
      _guard.NotifyBaseCalled(KEY_B);
      _guard.AssertBaseCalled();
      _guard.AssertBaseCalled();
      _guard.AssertBaseCalled();
      _guard.AssertBaseCalled();
    }

    [Test]
    [ExpectedException(typeof(BaseNotCalledException))]
    public void Recursive_BaseNotCalled1() {
      _guard.Begin(KEY_A);
      _guard.Begin(KEY_B);
      _guard.NotifyBaseCalled(KEY_B);
      _guard.AssertBaseCalled();
      _guard.AssertBaseCalled();
    }

    [Test]
    [ExpectedException(typeof(BaseNotCalledException))]
    public void Recursive_BaseNotCalled2() {
      _guard.Begin(KEY_A);
      _guard.NotifyBaseCalled(KEY_A);
      _guard.Begin(KEY_B);
      _guard.AssertBaseCalled();
      _guard.AssertBaseCalled();
    }

    [Test]
    [ExpectedException(typeof(BaseNotCalledException))]
    public void BaseNotCalled() {
      _guard.Begin(KEY_A);
      _guard.AssertBaseCalled();
    }

    [Test]
    [ExpectedException(typeof(WrongBaseCalledException))]
    public void WrongBaseCalled() {
      _guard.Begin(KEY_A);
      _guard.NotifyBaseCalled(KEY_B);
      _guard.AssertBaseCalled();
    }

    [Test]
    [ExpectedException(typeof(BeginNotCalledException))]
    public void BeginNotCalled_Nofity() {
      _guard.NotifyBaseCalled(KEY_B);
    }

    [Test]
    [ExpectedException(typeof(BeginNotCalledException))]
    public void BeginNotCalled_Assert() {
      _guard.AssertBaseCalled();
    }

  }
}
