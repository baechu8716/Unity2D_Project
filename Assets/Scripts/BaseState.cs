using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 상태가 상속해야 하는 추상 클래스입니다.
/// </summary>
public abstract class BaseState
{
    // 상태 컨텍스트(주로 PlayerControllerFSM)
    protected PlayerControllerFSM ctx;

    // 물리 업데이트를 해야 하는지 여부
    public virtual bool HasPhysics => false;

    protected BaseState(PlayerControllerFSM context)
    {
        ctx = context;
    }

    /// <summary>상태 진입 시 한 번 호출</summary>
    public virtual void Enter() { }

    /// <summary>입력 처리 단계에서 호출</summary>
    public virtual void HandleInput() { }

    /// <summary>논리(경계 조건 등) 처리 단계에서 호출</summary>
    public virtual void LogicUpdate() { }

    /// <summary>물리(FixedUpdate) 단계에서 호출</summary>
    public virtual void PhysicsUpdate() { }

    /// <summary>상태 종료 직전에 호출</summary>
    public virtual void Exit() { }
}
