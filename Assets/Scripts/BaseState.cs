using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��� ���°� ����ؾ� �ϴ� �߻� Ŭ�����Դϴ�.
/// </summary>
public abstract class BaseState
{
    // ���� ���ؽ�Ʈ(�ַ� PlayerControllerFSM)
    protected PlayerControllerFSM ctx;

    // ���� ������Ʈ�� �ؾ� �ϴ��� ����
    public virtual bool HasPhysics => false;

    protected BaseState(PlayerControllerFSM context)
    {
        ctx = context;
    }

    /// <summary>���� ���� �� �� �� ȣ��</summary>
    public virtual void Enter() { }

    /// <summary>�Է� ó�� �ܰ迡�� ȣ��</summary>
    public virtual void HandleInput() { }

    /// <summary>��(��� ���� ��) ó�� �ܰ迡�� ȣ��</summary>
    public virtual void LogicUpdate() { }

    /// <summary>����(FixedUpdate) �ܰ迡�� ȣ��</summary>
    public virtual void PhysicsUpdate() { }

    /// <summary>���� ���� ������ ȣ��</summary>
    public virtual void Exit() { }
}
