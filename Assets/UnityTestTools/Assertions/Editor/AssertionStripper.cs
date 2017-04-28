/****************************************************************************** 
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 * 
 * Leap Motion proprietary and  confidential.                                 * 
 *                                                                            * 
 * Use subject to the terms of the Leap Motion SDK Agreement available at     * 
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       * 
 * between Leap Motion and you, your company or other organization.           * 
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityTest;
using Object = UnityEngine.Object;

public class AssertionStripper
{
    [PostProcessScene]
    public static void OnPostprocessScene()
    {
        if (Debug.isDebugBuild) return;
        RemoveAssertionsFromGameObjects();
    }

    private static void RemoveAssertionsFromGameObjects()
    {
        var allAssertions = Resources.FindObjectsOfTypeAll(typeof(AssertionComponent)) as AssertionComponent[];
        foreach (var assertion in allAssertions)
        {
            Object.DestroyImmediate(assertion);
        }
    }
}
