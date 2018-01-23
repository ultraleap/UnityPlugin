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
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace Leap.Unity {

  public class BitConverterTests {

    private byte[] _bytes;

    [SetUp]
    public void SetUp() {
      _bytes = new byte[128];
      for (int i = 0; i < _bytes.Length; i++) {
        _bytes[i] = (byte)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
      }
    }

    [Test]
    public void TestToUInt16() {
      UInt16 expected = BitConverter.ToUInt16(_bytes, 0);
      UInt16 actual = BitConverterNonAlloc.ToUInt16(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromUInt16() {
      UInt16 value = (UInt16)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }

    [Test]
    public void TestToInt16() {
      Int16 expected = BitConverter.ToInt16(_bytes, 0);
      Int16 actual = BitConverterNonAlloc.ToInt16(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromInt16() {
      Int16 value = (Int16)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }

    [Test]
    public void TestToUInt32() {
      UInt32 expected = BitConverter.ToUInt32(_bytes, 0);
      UInt32 actual = BitConverterNonAlloc.ToUInt32(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromUInt32() {
      UInt32 value = (UInt32)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }

    [Test]
    public void TestToInt32() {
      Int32 expected = BitConverter.ToInt32(_bytes, 0);
      Int32 actual = BitConverterNonAlloc.ToInt32(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromInt32() {
      Int32 value = (Int32)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }

    [Test]
    public void TestToUInt64() {
      UInt64 expected = BitConverter.ToUInt64(_bytes, 0);
      UInt64 actual = BitConverterNonAlloc.ToUInt64(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromUInt64() {
      UInt64 value = (UInt64)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }

    [Test]
    public void TestToInt64() {
      Int64 expected = BitConverter.ToInt64(_bytes, 0);
      Int64 actual = BitConverterNonAlloc.ToInt64(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromInt64() {
      Int64 value = (Int64)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }

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

    [Test]
    public void TestToDouble() {
      Double expected = BitConverter.ToDouble(_bytes, 0);
      Double actual = BitConverterNonAlloc.ToDouble(_bytes, 0);

      Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TestFromDouble() {
      Double value = (Double)UnityEngine.Random.Range(float.MinValue, float.MaxValue);
      var actual = BitConverter.GetBytes(value);

      int offset = 0;
      BitConverterNonAlloc.GetBytes(value, _bytes, ref offset);

      Assert.That(offset, Is.EqualTo(actual.Length));
      Assert.That(_bytes.Take(offset), Is.EquivalentTo(actual));
    }
  }
}
