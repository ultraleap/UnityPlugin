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

  public interface ISupportsFeature<T> where T : LeapGraphicFeatureBase {
    /// <summary>
    /// Must be implemented by a renderer to report what level of support
    /// it has for all features of this type.  
    /// 
    /// The 'features' list will
    /// contain all features requested in priority order, and the 'info'
    /// list will come pre-filled with full-support info items.  The
    /// renderer must change these full-support items to a warning or
    /// error item to reflect what it is able to support.
    /// 
    /// This method will NEVER be called if there are 0 features of type T.
    /// </summary>
    void GetSupportInfo(List<T> features, List<SupportInfo> info);
  }
}
