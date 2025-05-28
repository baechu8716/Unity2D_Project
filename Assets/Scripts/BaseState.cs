using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState
{
    protected PlayerController player;

    public BaseState(PlayerController player)
    {
        this.player = player;
    }

    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
}