using System.Collections.Generic;
using UnityEngine;


public class BossStateMachine
{
    public BossBaseState CurrentState { get; private set; }
    private Dictionary<EBossState, BossBaseState> states = new Dictionary<EBossState, BossBaseState>();

    public BossStateMachine(BossController bossController) // BossController�� �޾� �� ���¿� �Ѱ��� �� �ֵ���
    {
        // ���µ��� ���⼭ ���� �����ϰų�, BossController���� AddState�� ȣ���Ͽ� �߰�
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

