using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace Leap.Unity.Generation {

  public class BitConverterTests {

    private byte[] _bytes;

    [SetUp]
    public void SetUp() {
      _bytes = new byte[128];
      for (int i = 0; i < _bytes.Length; i++) {
        _bytes[i] = (byte)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
      }
    }
    //BEGIN

    [Test]
    public void TestToSingle() {
      Single expected = BitConverter.ToSingle(_bytes, 0);
      Single actual = BitConverterNonAlloc.ToSingle(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromSingle() {
      Single value = (Single)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }
    //END
  }
}
