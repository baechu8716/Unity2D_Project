using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerControllerFSM;

public class StateMachine
{
    // ���� ���� ���� ����
    public BaseState Cur { get; private set; }

    // ���º��� Enter/Exit/Update ��������Ʈ
    private class StateData
    {
        public Action Enter, Exit, HandleInput, LogicUpdate, PhysicsUpdate;
    }

    private readonly Dictionary<EPlayerState, StateData> _states = new();

    /// <summary>
    /// ���� �ӽſ� ���¸� ����մϴ�.
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
    /// ���� ��ȯ
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
    /// ���� ������ HandleInput ȣ��
    /// </summary>
    public void HandleInput() => _states[Cur].HandleInput?.Invoke();

    /// <summary>
    /// ���� ������ LogicUpdate ȣ��
    /// </summary>
    public void LogicUpdate() => _states[Cur].LogicUpdate?.Invoke();

    /// <summary>
    /// ���� ������ PhysicsUpdate ȣ��
    /// </summary>
    public void PhysicsUpdate() => _states[Cur].PhysicsUpdate?.Invoke();

    
}
