/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  public interface ISupportsAddRemove {
    /// <summary>
    /// Must be implemented by a renderer to report that it 
    /// is able to support adding and removing graphics at runtime.
    /// 
    /// Will be called once per frame, with a list of indexes that are
    /// 'dirty'.  Each dirty index represents a graphic that might have
    /// changed completely because an ordering has changed.  Dirty
    /// indexes will not be included for new graphics, so you will also
    /// need to check to see if the graphics list has increased in size.
    /// </summary>
    void OnAddRemoveGraphics(List<int> dirtyIndexes);
  }
}
