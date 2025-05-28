using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerStatus : MonoBehaviour
{
    // �÷��̾� ����
    [field: SerializeField] public float WalkSpeed { get; private set; }
    [field: SerializeField] public float RunSpeed { get; private set; }
    [field: SerializeField] public float JumpForce { get; private set; }
    [field: SerializeField] public float DashSpeed { get; private set; }
    [field: SerializeField] public int MaxHP { get; private set; }

    // ���� �̺�Ʈ
    public ObservableProperty<bool> IsMoving { get; } = new();
    public ObservableProperty<bool> IsJumping{ get; } = new();
    public ObservableProperty<bool> IsDashing { get; } = new();
    public ObservableProperty<bool> IsAiming { get; } = new();
    public ObservableProperty<bool> IsAttacking { get; } = new();

    // ü�� �̺�Ʈ
    //public ObservableProperty<int> CurrentHP { get; } = new();

    //private void Awake()
    //{
    //    CurrentHP.Value = MaxHP;
    //}
}
