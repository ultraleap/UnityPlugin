using Leap;
using Leap.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandShapeObserver : IStateObserver
{
    private IHandShapeDefinition _targetHandShape;
    private readonly IHandShapeRecognizer _shapeRecognizer;
    private readonly IServiceProvider _serviceProvider;

    public event EventHandler OnStateObserved;
    public event EventHandler OnStateLost;

    void FromHand(Hand hand)
    {
    }

    HandShapeDefinition TargetShape
    {
        set
        {
            _targetHandShape = value;
        }
    }

    public bool IsObserved => throw new NotImplementedException();

    public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
