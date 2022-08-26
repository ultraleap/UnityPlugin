using JetBrains.Annotations;
using Leap;
using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandProximity : IStateObserver
{
    public bool IsObserved => throw new NotImplementedException();

    public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public event EventHandler OnStateObserved;
    public event EventHandler OnStateLost;


    private HandProximityJoint _thumbDistal;

    private HandProximityJoint _indexDistal;

    public HandProximityJoint ThumbDistal
    {
        get
        {
            return this._thumbDistal;
        }
    }

    public HandProximityJoint IndexDistal
    {
        get
        {
            return this._indexDistal;
        }
    }

    public HandProximity()
    {

    }
}

public class HandProximityJoint
{
    private readonly HandProximity _parent;
    private float _distance;
    private HandProximityJoint _target;

    public HandProximityJoint(HandProximity parent) 
    { 
        this._parent = parent;   
    }

    public HandProximityJoint IsWithin(float distance)
    {
        this._distance = distance;
        return this;
    }

    internal HandProximityJoint Of(HandProximityJoint target)
    {
        return this;
    }
}