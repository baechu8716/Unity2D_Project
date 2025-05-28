using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerControllerFSM;

public class StateMachine
{
    // 현재 실행 중인 상태
    public BaseState Cur { get; private set; }

    // 상태별로 Enter/Exit/Update 델리게이트
    private class StateData
    {
        public Action Enter, Exit, HandleInput, LogicUpdate, PhysicsUpdate;
    }

    private readonly Dictionary<EPlayerState, StateData> _states = new();

    /// <summary>
    /// 상태 머신에 상태를 등록합니다.
    /// </summary>
    public void AddState(
        EPlayerState key,
        Action onEnter,
        Action onHandleInput = null,
        Action onLogicUpdate = null,
        Action onExit = null,
        Action onPhysicsUpdate = null)
    {
        _states[key] = new StateData
        {
            Enter = onEnter,
            HandleInput = onHandleInput,
            LogicUpdate = onLogicUpdate,
            Exit = onExit,
            PhysicsUpdate = onPhysicsUpdate
        };
    }

    /// <summary>
    /// 상태 전환
    /// </summary>
    public void ChangeState(EPlayerState next)
    {
        if (Cur == null || Cur != next)
        {
            Cur?.Exit();
            Cur = next;
            _states[next].Enter?.Invoke();
        }
    }

    /// <summary>
    /// 현재 상태의 HandleInput 호출
    /// </summary>
    public void HandleInput() => _states[Cur].HandleInput?.Invoke();

    /// <summary>
    /// 현재 상태의 LogicUpdate 호출
    /// </summary>
    public void LogicUpdate() => _states[Cur].LogicUpdate?.Invoke();

    /// <summary>
    /// 현재 상태의 PhysicsUpdate 호출
    /// </summary>
    public void PhysicsUpdate() => _states[Cur].PhysicsUpdate?.Invoke();

    
}
