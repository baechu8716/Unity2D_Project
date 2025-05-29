using UnityEngine;


public class IdleState : BaseState
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("Idle");
        player.Animator.Update(0f);
    }

    public override void Execute()
    {
        if (player == null || player.Movement == null) return;

        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
            player.ChangeState(EPlayerState.Move);
        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded())
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift) && player.CanRoll())
            player.ChangeState(EPlayerState.Roll);
        if (!player.Movement.IsGrounded() && player.Movement.VerticalVelocity < 0)
            player.ChangeState(EPlayerState.Fall); // 공중 낙하 시 FallState로
    }

    public override void Exit() { }
}

public class MoveState : BaseState
{
    public MoveState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("Run");
        player.Animator.Update(0f);
    }

    public override void Execute()
    {
        if (player == null || player.Movement == null) return;

        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        if (moveInput == 0)
            player.ChangeState(EPlayerState.Idle);
        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded())
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift) && player.CanRoll())
            player.ChangeState(EPlayerState.Roll);
        if (!player.Movement.IsGrounded() && player.Movement.VerticalVelocity < 0)
            player.ChangeState(EPlayerState.Fall); // 공중 낙하 시 FallState로
    }

    public override void Exit() { }
}

public class JumpState : BaseState
{
    private float jumpTime;
    private readonly float minJumpAnimTime = 0.3f; // 적절한 시간으로 조정
    private float previousYVelocity; // 이전 프레임의 yVelocity

    public JumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Movement.Jump();
        jumpTime = 0f;
        player.Animator.Play("Jump", 0, 0f);
        player.Animator.Update(0f);
        player.HasPlayedJumpAnimation = true;
        previousYVelocity = player.Movement.VerticalVelocity;
    }

    public override void Execute()
    {
        jumpTime += Time.deltaTime;
        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        float yVelocity = player.Movement.VerticalVelocity;

        if (jumpTime >= minJumpAnimTime && yVelocity < previousYVelocity && yVelocity < -5f && !player.Movement.IsGrounded())
            player.ChangeState(EPlayerState.Fall);

        // 일정 시간 이후에만 착지를 체크
        if (jumpTime >= minJumpAnimTime && player.Movement.IsGrounded())
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
        player.Animator.Play("Fall", 0, 0f);
        player.Animator.Update(0f);
    }

    public override void Execute()
    {
        if (player == null || player.Movement == null) return;

        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        if (player.Movement.IsGrounded())
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
        player.Animator.Play("Roll", 0, 0f);
        player.Animator.Update(0f);
        player.Movement.Roll();
        player.OnRoll();
        player.SetInvincibility(true);
        rollTimer = 0f;
    }

    public override void Execute()
    {
        if (player == null) return;

        rollTimer += Time.deltaTime;
        if (rollTimer >= rollDuration)
            player.ChangeState(EPlayerState.Idle);
    }

    public override void Exit()
    {
        player.SetInvincibility(false);
    }
}