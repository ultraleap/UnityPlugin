using System;

public interface IStateObserver
{
    event EventHandler OnStateObserved;
    event EventHandler OnStateLost;

    bool IsObserved { get; }

    bool Enabled { get; set; }
}