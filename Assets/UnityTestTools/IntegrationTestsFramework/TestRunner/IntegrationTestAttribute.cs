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
using System.IO;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class IntegrationTestAttribute : Attribute
{
    private readonly string m_Path;

    public IntegrationTestAttribute(string path)
    {
        if (path.EndsWith(".unity"))
            path = path.Substring(0, path.Length - ".unity".Length);
        m_Path = path;
    }

    public bool IncludeOnScene(string scenePath)
    {
        if (scenePath == m_Path) return true;
        var fileName = Path.GetFileNameWithoutExtension(scenePath);
        return fileName == m_Path;
    }
}
