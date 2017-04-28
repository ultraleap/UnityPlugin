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
using UnityTest;

public interface ITestResult
{
    TestResultState ResultState { get; }

    string Message { get; }

    string Logs { get; }

    bool Executed { get; }

    string Name { get; }

    string FullName { get; }

    string Id { get; }

    bool IsSuccess { get; }

    double Duration { get; }

    string StackTrace { get; }
    
    bool IsIgnored { get; }
}
