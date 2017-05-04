/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayBuffer<T> {

  private RingBuffer<T> buffer;

  /// <summary> Returns the underlying Buffer object. </summary>
  public RingBuffer<T> Buffer { get { return buffer; } }

  public DelayBuffer(int bufferSize) {
    buffer = new RingBuffer<T>(bufferSize);
  }

  /// <summary> Returns true if the buffer was full and out "delayedT" will
  /// contain the oldest value in the buffer, otherwise returns false. </summary>
  public bool Add(T t, out T delayedT) {
    bool willOutputValue;
    if (buffer.IsFull) {
      willOutputValue = true;
      delayedT = buffer.GetOldest();
    }
    else {
      willOutputValue = false;
      delayedT = default(T);
    }
    buffer.Add(t);
    return willOutputValue;
  }

}
