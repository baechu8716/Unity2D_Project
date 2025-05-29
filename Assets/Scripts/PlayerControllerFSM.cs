using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : BaseState
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("idle");
        player.Animator.Update(0f); // 즉시 업데이트
    }

    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
            player.ChangeState(EPlayerState.Move);
        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded())
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift) && player.CanRoll())
            player.ChangeState(EPlayerState.Roll);
    }

    public override void Exit() { }
}

public class MoveState : BaseState
{
    public MoveState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("run");
        player.Animator.Update(0f); // 즉시 업데이트
    }

    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        if (moveInput == 0)
            player.ChangeState(EPlayerState.Idle);
        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded())
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift) && player.CanRoll())
            player.ChangeState(EPlayerState.Roll);
    }

    public override void Exit() { }
}

public class JumpState : BaseState
{
    public JumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Movement.Jump();

        if (!player.HasPlayedJumpAnimation)
        {
            player.Animator.Play("j_up", 0, 0f);
            player.Animator.Update(0f); // 즉시 업데이트
            player.HasPlayedJumpAnimation = true;
        }
    }

    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        float yVelocity = player.Movement.VerticalVelocity;
        if (yVelocity < 0)
        {
            player.ChangeState(EPlayerState.Fall);
        }

        if (player.Status.IsGrounded.Value)
        {
            player.HasPlayedJumpAnimation = false;
            player.ChangeState(EPlayerState.Idle);
        }
    }

    public override void Exit() { }
}

public class FallState : BaseState
{
    public FallState(PlayerController player) : base(player) { }

    public override void Enter()
    {

        player.Animator.Play("j_down", 0, 0f);
        player.Animator.Update(0f); // 즉시 업데이트
    }

    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        if (player.Status.IsGrounded.Value)
        {
            player.HasPlayedJumpAnimation = false;
            player.ChangeState(EPlayerState.Idle);
        }
    }

    public override void Exit() { }
}

public class RollState : BaseState
{
    private float rollDuration = 0.5f;
    private float rollTimer;

    public RollState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        if (!player.CanRoll()) return;
        player.Animator.Play("roll", 0, 0f);
        player.Animator.Update(0f); // 즉시 업데이트
        player.Movement.Roll();
        player.OnRoll();
        rollTimer = 0f;
    }

    public override void Execute()
    {
        rollTimer += Time.deltaTime;
        if (rollTimer >= rollDuration)
        {
            player.ChangeState(EPlayerState.Idle);
        }
    }

    public override void Exit() { }
}