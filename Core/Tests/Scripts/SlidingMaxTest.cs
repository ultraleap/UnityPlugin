/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;

namespace Leap.Unity.Tests {

  public class SlidingMaxTest {
    public const int MAX_HISTORY = 64;

    private SlidingMax _slidingMax;

    [SetUp]
    public void Setup() {
      _slidingMax = new SlidingMax(MAX_HISTORY);
    }

    [TearDown]
    public void Teardown() {
      _slidingMax = null;
    }

    [Test]
    public void IsFunctional() {
      List<float> list = new List<float>();

      for (int i = 0; i < 1000; i++) {
        float newValue = Random.value;

        _slidingMax.AddValue(newValue);

        list.Add(newValue);
        while (list.Count > MAX_HISTORY) {
          list.RemoveAt(0);
        }

        float max = list[0];
        for (int j = 1; j < list.Count; j++) {
          max = Mathf.Max(max, list[j]);
        }

        Assert.That(max, Is.EqualTo(_slidingMax.Max));
      }
    }
  }
}
