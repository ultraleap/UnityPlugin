/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
using System;
using System.Reflection;


namespace LeapInternal{
    public static class Logger
    {
        //
        // Summary:
        //     Logs message to the a Console.
        public static void Log(object message)
        {
            #if DEBUG
                #if UNITY_EDITOR
                    UnityEngine.Debug.Log(message);
                #else
                    Console.WriteLine(message);
                #endif
            #endif
        }
            
        public static void LogStruct(object thisObject, string title = "")
        {
            try
            {
                if(!thisObject.GetType().IsValueType){
                    Logger.Log (title + " ---- Trying to log non-struct with struct logger");
                    return;
                }
                Logger.Log (title + " ---- " + thisObject.GetType().ToString());
                FieldInfo[] fieldInfos;
                fieldInfos = thisObject.GetType().GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic // Get public and non-public
                    | BindingFlags.Static | BindingFlags.Instance  // Get instance + static
                    | BindingFlags.FlattenHierarchy); // Search up the hierarchy

                // write member names
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    string value = fieldInfo.GetValue(thisObject).ToString();
                    Logger.Log(" -------- Name: " + fieldInfo.Name + ", Value = " + value);
                }
            }
            catch (Exception exception)
            {
                Logger.Log (exception.Message);
            }
            
        }
    }
}
