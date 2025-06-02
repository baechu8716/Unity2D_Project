using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossBaseState 
{
    protected BossController boss;
    protected BossStateMachine stateMachine; // ���� ������ ���� StateMachine ���� �߰�

    public BossBaseState(BossController boss, BossStateMachine stateMachine)
    {
        this.boss = boss;
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Execute(); // Unity�� Updateó�� �� ������ ȣ��
    public abstract void Exit();
}
