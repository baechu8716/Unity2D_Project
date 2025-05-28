using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : BaseState
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("idle");
    }

    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
            player.ChangeState(EPlayerState.Move);
        if (Input.GetButtonDown("Jump"))
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift))
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
    }

    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        if (moveInput == 0)
            player.ChangeState(EPlayerState.Idle);
        if (Input.GetButtonDown("Jump"))
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift))
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
            player.HasPlayedJumpAnimation = true;
        }
    }

    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        float yVelocity = player.Movement.VerticalVelocity;
        if (yVelocity < 0)
            player.ChangeState(EPlayerState.Fall);
    }

    public override void Exit() { }
}

public class FallState : BaseState
{
    public FallState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("j_down");
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
    public RollState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("roll");
        player.Movement.Roll();
    }

    public override void Execute()
    {
        if (!player.Animator.GetCurrentAnimatorStateInfo(0).IsName("roll"))
            player.ChangeState(EPlayerState.Idle);
    }

    public override void Exit() { }
}