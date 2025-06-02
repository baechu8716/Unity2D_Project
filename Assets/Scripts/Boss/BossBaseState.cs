using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossBaseState 
{
    protected BossController boss;
    protected BossStateMachine stateMachine; // 상태 변경을 위해 StateMachine 참조 추가

    public BossBaseState(BossController boss, BossStateMachine stateMachine)
    {
        this.boss = boss;
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Execute(); // Unity의 Update처럼 매 프레임 호출
    public abstract void Exit();
}
