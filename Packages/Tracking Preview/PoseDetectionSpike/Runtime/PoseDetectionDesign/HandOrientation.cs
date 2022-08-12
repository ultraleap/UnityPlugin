using Leap;
using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HandOrientation : IStateObserver
{
    public bool IsObserved => throw new NotImplementedException();

    public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public event EventHandler OnStateObserved;
    public event EventHandler OnStateLost;
}