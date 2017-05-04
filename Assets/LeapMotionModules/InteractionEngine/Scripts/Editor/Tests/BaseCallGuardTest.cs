/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
    public void Recursive_BaseNotCalled1() {
      Assert.That(() => {
        _guard.Begin(KEY_A);
        _guard.Begin(KEY_B);
        _guard.NotifyBaseCalled(KEY_B);
        _guard.AssertBaseCalled();
        _guard.AssertBaseCalled();
      }, Throws.InstanceOf<BaseNotCalledException>());
    }

    [Test]
    public void Recursive_BaseNotCalled2() {
      Assert.That(() => {
        _guard.Begin(KEY_A);
        _guard.NotifyBaseCalled(KEY_A);
        _guard.Begin(KEY_B);
        _guard.AssertBaseCalled();
        _guard.AssertBaseCalled();
      }, Throws.InstanceOf<BaseNotCalledException>());
    }

    [Test]
    public void BaseNotCalled() {
      Assert.That(() => {
        _guard.Begin(KEY_A);
        _guard.AssertBaseCalled();
      }, Throws.InstanceOf<BaseNotCalledException>());
    }

    [Test]
    public void WrongBaseCalled() {
      Assert.That(() => {
        _guard.Begin(KEY_A);
        _guard.NotifyBaseCalled(KEY_B);
        _guard.AssertBaseCalled();
      }, Throws.InstanceOf<WrongBaseCalledException>());
    }

    [Test]
    public void BeginNotCalled_Nofity() {
      Assert.That(() => {
        _guard.NotifyBaseCalled(KEY_B);
      }, Throws.InstanceOf<BeginNotCalledException>());
    }

    [Test]
    public void BeginNotCalled_Assert() {
      Assert.That(() => {
        _guard.AssertBaseCalled();
      }, Throws.InstanceOf<BeginNotCalledException>());
    }

  }
}
