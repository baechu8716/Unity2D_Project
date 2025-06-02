using System.Collections.Generic;
using UnityEngine;


public class BossStateMachine
{
    public BossBaseState CurrentState { get; private set; }
    private Dictionary<EBossState, BossBaseState> states = new Dictionary<EBossState, BossBaseState>();

    public BossStateMachine(BossController bossController) // BossController를 받아 각 상태에 넘겨줄 수 있도록
    {
        // 상태들을 여기서 직접 생성하거나, BossController에서 AddState를 호출하여 추가
    }

    public void AddState(EBossState stateType, BossBaseState state)
    {
        states[stateType] = state;
    }

    public void ChangeState(EBossState newStateKey)
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

