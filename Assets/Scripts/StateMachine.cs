using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public BaseState currentState;
    private Dictionary<EPlayerState, BaseState> states = new Dictionary<EPlayerState, BaseState>();

    public void AddState(EPlayerState stateType, BaseState state)
    {
        states[stateType] = state;
    }

    public void ChangeState(EPlayerState newState)
    {
        if (currentState != null)
            currentState.Exit();

        currentState = states[newState];
        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Execute();
    }
}