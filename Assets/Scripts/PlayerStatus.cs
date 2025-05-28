using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerStatus : MonoBehaviour
{
    // 플레이어 스탯
    [field: SerializeField] public float WalkSpeed { get; private set; }
    [field: SerializeField] public float RunSpeed { get; private set; }
    [field: SerializeField] public float JumpForce { get; private set; }
    [field: SerializeField] public float DashSpeed { get; private set; }
    [field: SerializeField] public int MaxHP { get; private set; }

    // 상태 이벤트
    public ObservableProperty<bool> IsMoving { get; } = new();
    public ObservableProperty<bool> IsJumping{ get; } = new();
    public ObservableProperty<bool> IsDashing { get; } = new();
    public ObservableProperty<bool> IsAiming { get; } = new();
    public ObservableProperty<bool> IsAttacking { get; } = new();

    // 체력 이벤트
    //public ObservableProperty<int> CurrentHP { get; } = new();

    //private void Awake()
    //{
    //    CurrentHP.Value = MaxHP;
    //}
}
