/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
