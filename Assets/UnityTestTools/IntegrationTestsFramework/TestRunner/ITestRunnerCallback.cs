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
using UnityEngine;

namespace UnityTest.IntegrationTestRunner
{
    public interface ITestRunnerCallback
    {
        void RunStarted(string platform, List<TestComponent> testsToRun);
        void RunFinished(List<TestResult> testResults);
        void AllScenesFinished();
        void TestStarted(TestResult test);
        void TestFinished(TestResult test);
        void TestRunInterrupted(List<ITestComponent> testsNotRun);
    }
}
