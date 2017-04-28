/****************************************************************************** 
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 * 
 * Leap Motion proprietary and  confidential.                                 * 
 *                                                                            * 
 * Use subject to the terms of the Leap Motion SDK Agreement available at     * 
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       * 
 * between Leap Motion and you, your company or other organization.           * 
 ******************************************************************************/

using System;
using System.Reflection;
using UnityEditor;

namespace UnityTest
{
    public static class GuiHelper
    {
        public static bool GetConsoleErrorPause()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
            Type type = assembly.GetType("UnityEditorInternal.LogEntries");
            PropertyInfo method = type.GetProperty("consoleFlags");
            var result = (int)method.GetValue(new object(), new object[] { });
            return (result & (1 << 2)) != 0;
        }

        public static void SetConsoleErrorPause(bool b)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
            Type type = assembly.GetType("UnityEditorInternal.LogEntries");
            MethodInfo method = type.GetMethod("SetConsoleFlag");
            method.Invoke(new object(), new object[] { 1 << 2, b });
        }
    }
}
