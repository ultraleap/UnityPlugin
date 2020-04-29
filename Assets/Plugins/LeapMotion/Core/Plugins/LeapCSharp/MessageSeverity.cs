/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap {
  /// <summary>
  /// Reports whether the message is for
  /// a severe failure, a recoverable warning, or a status change.
  /// @since 3.0
  /// </summary>
  public enum MessageSeverity {
    MESSAGE_UNKNOWN = 0,
    MESSAGE_CRITICAL = 1,
    MESSAGE_WARNING = 2,
    /** A verbose, informational message */
    MESSAGE_INFORMATION = 3
  }
}
