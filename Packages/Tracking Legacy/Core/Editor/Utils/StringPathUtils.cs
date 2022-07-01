/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.IO;

namespace Leap.Unity.StringPathUtils
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public static class StringPathUtilExtensions
    {

        /// <summary> Returns whether the argument path string actually points to
        /// a file in the OS filesystem. </summary>
        public static bool IsValidReadPath(this string pathString)
        {
            return File.Exists(pathString);
        }

        /// <summary> Returns whether the argument path string is a path that can
        /// be written to. Warning, `true` will be returned even if writing would
        /// overwrite an existing file at that path. </summary>
        public static bool IsValidWritePath(this string pathString)
        {
            if (File.Exists(pathString)) { return true; }
            else
            {
                try
                {
                    File.Create(pathString);
                    File.Delete(pathString);
                    return true;
                }
                catch (System.Exception) { return false; }
            }
        }

    }

}