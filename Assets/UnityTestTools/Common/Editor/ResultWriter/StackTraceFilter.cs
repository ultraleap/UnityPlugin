/****************************************************************************** 
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 * 
 * Leap Motion proprietary and  confidential.                                 * 
 *                                                                            * 
 * Use subject to the terms of the Leap Motion SDK Agreement available at     * 
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       * 
 * between Leap Motion and you, your company or other organization.           * 
 ******************************************************************************/

// ****************************************************************
// Based on nUnit 2.6.2 (http://www.nunit.org/)
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityTest
{
    /// <summary>
    /// Summary description for StackTraceFilter.
    /// </summary>
    public class StackTraceFilter
    {
        public static string Filter(string stack)
        {
            if (stack == null) return null;
            var sw = new StringWriter();
            var sr = new StringReader(stack);

            try
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!FilterLine(line))
                        sw.WriteLine(line.Trim());
                }
            }
            catch (Exception)
            {
                return stack;
            }
            return sw.ToString();
        }

        static bool FilterLine(string line)
        {
            string[] patterns =
            {
                "NUnit.Core.TestCase",
                "NUnit.Core.ExpectedExceptionTestCase",
                "NUnit.Core.TemplateTestCase",
                "NUnit.Core.TestResult",
                "NUnit.Core.TestSuite",
                "NUnit.Framework.Assertion",
                "NUnit.Framework.Assert",
                "System.Reflection.MonoMethod"
            };

            for (int i = 0; i < patterns.Length; i++)
            {
                if (line.IndexOf(patterns[i]) > 0)
                    return true;
            }

            return false;
        }
    }
}
