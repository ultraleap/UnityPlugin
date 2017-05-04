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

namespace Leap.Unity {

  public class TransformUtilTests {

    private GameObject _gameObject;
    private GameObject _child;

    [SetUp]
    public void Setup() {
      _gameObject = new GameObject("__TEST OBJECT__");
      _child = new GameObject("__CHILD OBJECT__");
      _child.transform.SetParent(_gameObject.transform);
      _gameObject.transform.rotation = Quaternion.Euler(45, 123, 888);
      _child.transform.rotation = Quaternion.Euler(2, 44, 99);
    }

    [TearDown]
    public void Teardown() {
      Object.DestroyImmediate(_child);
      Object.DestroyImmediate(_gameObject);
      _child = null;
      _gameObject = null;
    }

    [Test]
    public void TransformRotationTest() {
      AssertAlmostEqual(_gameObject.transform.TransformRotation(_child.transform.localRotation),
                        _child.transform.rotation);
    }

    [Test]
    public void InverseTransformRotationTest() {
      AssertAlmostEqual(_gameObject.transform.InverseTransformRotation(_child.transform.rotation),
                        _child.transform.localRotation);
    }

    private static void AssertAlmostEqual(Quaternion a, Quaternion b) {
      for (int i = 0; i < 4; i++) {
        Assert.That(a[i], Is.EqualTo(b[i]).Within(10).Ulps);
      }
    }
  }
}
