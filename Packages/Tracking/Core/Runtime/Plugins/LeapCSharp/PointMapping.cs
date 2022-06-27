/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap
{
#pragma warning disable 0618
    public struct PointMapping
    {
        public long frameId;
        public long timestamp;
        public Vector[] points;
        public uint[] ids;
    }
#pragma warning restore 0618
}