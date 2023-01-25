/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

namespace Leap.Unity.RuntimeGizmos
{

    /// <summary>
    /// This class controls the display of all the runtime gizmos
    /// that are either attatched to this gameObject, or a child of
    /// this gameObject.  Enable this component to allow the gizmos
    /// to be drawn, and disable it to hide them.
    /// </summary>
    public class RuntimeGizmoToggle : MonoBehaviour
    {
        public void OnEnable() { }
    }
}