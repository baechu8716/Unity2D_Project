using System.Collections.Generic;
using UnityEngine;


public class BossStateMachine
{
    public BossBaseState CurrentState { get; private set; }
    private Dictionary<EBossState, BossBaseState> states = new Dictionary<EBossState, BossBaseState>();
    private BossController ownerBoss; // BossController 참조

    public BossStateMachine(BossController boss) // 생성자에서 BossController 받기
    {
        this.ownerBoss = boss;
    }

    public void AddState(EBossState stateType, BossBaseState state)
    {
        states[stateType] = state;
    }

    public void ChangeState(EBossState newStateKey)
    {
        if (!states.ContainsKey(newStateKey))
        {
            Debug.LogError($"BossStateMachine: State {newStateKey} not found!");
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

