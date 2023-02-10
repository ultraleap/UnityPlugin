/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{
    /// <summary>
    /// Reports whether the message is for
    /// a severe failure, a recoverable warning, or a status change.
    /// @since 3.0
    /// </summary>
    public enum MessageSeverity
    {
        MESSAGE_UNKNOWN = 0,
        MESSAGE_CRITICAL = 1,
        MESSAGE_WARNING = 2,
        /** A verbose, informational message */
        MESSAGE_INFORMATION = 3
    }
}