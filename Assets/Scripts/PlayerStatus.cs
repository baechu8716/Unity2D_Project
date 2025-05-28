using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerStatus : MonoBehaviour
{
    [field: SerializeField] public float WalkSpeed { get; private set; } = 3f;
    [field: SerializeField] public float RunSpeed { get; private set; } = 6f;
    [field: SerializeField] public float JumpForce { get; private set; } = 12f;
    [field: SerializeField] public float RollSpeed { get; private set; } = 15f;
    [field: SerializeField] public int MaxHP { get; private set; } = 5;

    // �ִϸ����Ϳ� �Ķ����
    public ObservableProperty<float> MoveSpeed { get; } = new();   // |vx|
    public ObservableProperty<float> VerticalSpeed { get; } = new();   // vy
    public ObservableProperty<bool> IsGrounded { get; } = new();

    // ���� �÷��� (FSM ���̿�)
    public ObservableProperty<bool> RollTriggered { get; } = new();
    public ObservableProperty<bool> JumpTriggered { get; } = new();
    public ObservableProperty<bool> AimTriggered { get; } = new();
    public ObservableProperty<bool> AttackTriggered { get; } = new();

    // ü��
    public ObservableProperty<int> CurrentHP { get; } = new();

    private void Awake()
    {
        CurrentHP.Value = MaxHP;
    }
}

