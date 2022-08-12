using Leap;
using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDelayedObserver : IStateObserver
{
    TimeSpan Delay { get; set; }
    void Reset();
}