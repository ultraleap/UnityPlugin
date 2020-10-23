/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
