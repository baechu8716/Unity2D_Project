using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public BaseState CurrentState { get; private set; }
    private Dictionary<EPlayerState, BaseState> states = new Dictionary<EPlayerState, BaseState>();

    public void AddState(EPlayerState stateType, BaseState state)
    {
        states[stateType] = state;
    }

    public void ChangeState(EPlayerState newStateKey)
    {
        if (!states.ContainsKey(newStateKey))
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = states[newStateKey];
        CurrentState.Enter();
    }

    public void Update()
    {
        CurrentState?.Execute();
    }
}