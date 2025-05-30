using System.Linq;
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
        float moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput != 0)
            player.ChangeState(EPlayerState.Move);
        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded())
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift) && player.CanRoll())
            player.ChangeState(EPlayerState.Roll);
        if (!player.Movement.IsGrounded() && player.Movement.VerticalVelocity < 0)
            player.ChangeState(EPlayerState.Fall);
        if (Input.GetMouseButtonDown(1))
            player.ChangeState(EPlayerState.Attack);
        if (Input.GetMouseButtonDown(0) && player.CanMeleeAttack())
        {
            player.ChangeState(EPlayerState.MeleeAttack);
        }
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
        float moveInput = Input.GetAxisRaw("Horizontal");
        player.Movement.Move(moveInput);

        if (moveInput == 0)
            player.ChangeState(EPlayerState.Idle);
        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded())
            player.ChangeState(EPlayerState.Jump);
        if (Input.GetKeyDown(KeyCode.LeftShift) && player.CanRoll())
            player.ChangeState(EPlayerState.Roll);
        if (!player.Movement.IsGrounded() && player.Movement.VerticalVelocity < 0)
            player.ChangeState(EPlayerState.Fall);
        if (Input.GetMouseButtonDown(1))
            player.ChangeState(EPlayerState.Attack);
        if (Input.GetMouseButtonDown(0) && player.CanMeleeAttack())
        {
            player.ChangeState(EPlayerState.MeleeAttack);
        }
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

        if (Input.GetMouseButtonDown(1))
            player.ChangeState(EPlayerState.Attack);

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

public class AttackState : BaseState
{
    private float attackTimer = 0f;
    private bool waitingForFire = false; // 활을 당기고 발사를 기다리는 상태
    private bool arrowFired = false;     // 화살이 발사되었는지 여부

    private AnimationClip attackClip;
    private float frameRate;
    private float drawFrameTime;      // 활 당기기 완료 시간 (초)
    private float totalAnimTime;      // 전체 Attack 애니메이션 시간 (초)
    private float normalizedDrawTime; // 정규화된 활 당기기 완료 시간

    public AttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        attackTimer = 0f;
        waitingForFire = false;
        arrowFired = false;

        player.Animator.Play("Attack", 0, 0f); // 애니메이션을 처음부터 재생
        player.Animator.speed = 1f;

        attackClip = player.Animator.runtimeAnimatorController.animationClips
            .FirstOrDefault(c => c.name == "Attack");

        if (attackClip != null)
        {
            frameRate = attackClip.frameRate > 0 ? attackClip.frameRate : 60f;
            drawFrameTime = 40f / frameRate;
            totalAnimTime = attackClip.length;
            if (totalAnimTime > 0)
            {
                normalizedDrawTime = drawFrameTime / totalAnimTime;
            }
            else
            {
                normalizedDrawTime = 0.5f; totalAnimTime = 1f; 
            }
        }
        else
        {
            frameRate = 60f; drawFrameTime = 40f / frameRate; totalAnimTime = 1f; normalizedDrawTime = drawFrameTime / totalAnimTime; 
        }
    }

    public override void Execute()
    {
        attackTimer += Time.deltaTime;
        AnimatorStateInfo currentStateInfo = player.Animator.GetCurrentAnimatorStateInfo(0);

        if (!currentStateInfo.IsName("Attack"))
        {
            // 현재 AttackState이지만, 애니메이터가 Attack 애니메이션을 재생하고 있지 않다면
            // (예: 매우 짧은 전환 중에 다른 애니메이션이 잠깐 재생될 가능성)
            // 일단은 아무것도 안 하거나, Idle로 변경
            return;
        }

        float currentNormalizedTime = currentStateInfo.normalizedTime;

        // 1. 활 당기기 (아직 발사 전, 조준 대기 상태도 아님)
        if (!arrowFired && !waitingForFire)
        {
            if (currentNormalizedTime >= normalizedDrawTime)
            {
                player.Animator.speed = 0f; // 목표 프레임에서 애니메이션 정지
                waitingForFire = true;     // 발사 대기 상태로 변경
            }
            // 활 당기는 중 우클릭 떼면 공격 취소
            else if (Input.GetMouseButtonUp(1))
            {
                player.Animator.speed = 1f;
                player.ChangeState(EPlayerState.Idle);
                return;
            }
        }

        // 2. 발사 대기 중 (애니메이션 멈춰있고, 아직 발사 전)
        if (waitingForFire && !arrowFired)
        {
            // 좌클릭으로 발사
            if (Input.GetMouseButtonDown(0))
            {
                player.FireArrow();          // 화살 발사
                player.Animator.speed = 1f;  // 나머지 애니메이션 재생
                arrowFired = true;          // 화살 발사됨 표시
                waitingForFire = false;     // 더는 발사 대기 상태가 아님
            }
            // 발사 대기 중 우클릭 떼면 공격 취소
            else if (Input.GetMouseButtonUp(1))
            {
                player.Animator.speed = 1f;
                player.ChangeState(EPlayerState.Idle);
                return;
            }
        }

        // 3. 화살 발사 후 (애니메이션 완료 또는 연속 공격을 위한 재조준)
        if (arrowFired)
        {
            // 애니메이션이 완전히 끝났다면 (normalizedTime >= 1.0f)
            if (currentNormalizedTime >= 1.0f)
            {
                if (Input.GetMouseButton(1)) // 우클릭이 여전히 눌려있다면 (연속 공격)
                {
                    // 상태를 초기화하여 다시 활 당기기부터 시작
                    attackTimer = 0f;
                    waitingForFire = false; // 이 값은 다음 프레임의 첫 번째 if 블록에서 true로 설정될 것임
                    arrowFired = false;
                    player.Animator.Play("Attack", 0, 0f); // 애니메이션을 처음부터 다시
                    player.Animator.speed = 1f;
                    // 필요한 변수만 리셋하고 애니메이션을 다시 시작
                }
                else // 우클릭이 떨어졌다면 Idle 상태로
                {
                    player.ChangeState(EPlayerState.Idle);
                }
            }
        }
    }

    public override void Exit()
    {
        player.Animator.speed = 1f; // 상태를 나갈 때 항상 애니메이션 속도 복원
    }

    public bool IsWaitingForFire()
    {
        return waitingForFire;
    }
}

public class MeleeAttackState : BaseState
{
    private float attackAnimDuration; // 애니메이션 길이를 저장할 변수
    private readonly float ATTACK_COOLDOWN = 1f; // 근접 공격 쿨타임 (예: 1초)

    // 공격 판정 관련 (PlayerController에서 처리하도록 위임 가능)
    // private readonly float ATTACK_RANGE = 1.5f;
    // private Transform attackPoint; // PlayerController에서 할당받거나 찾아야 함
    // private LayerMask hittableLayers; // PlayerController에서 설정

    private float stateTimer;       // 상태가 시작된 후 경과 시간
    private bool damageDealt;       // 이번 공격에서 데미지를 이미 입혔는지 여부 (단일 타격용)
    private float damageStartTime = 0.1f; // 애니메이션 시작 후 데미지 판정 시작 시간
    private float damageEndTime = 0.4f;   // 데미지 판정 종료 시간

    public MeleeAttackState(PlayerController player) : base(player)
    {
        // attackPoint = player.MeleeAttackPoint; // PlayerController에 이런 참조가 있다고 가정
        // hittableLayers = player.EnemyLayers;   // PlayerController에 이런 참조가 있다고 가정
    }

    public override void Enter()
    {
        stateTimer = 0f;
        damageDealt = false;
        player.Animator.Play("MeleeAttack");
        player.Animator.Update(0f); // 애니메이션 즉시 반영

        // 애니메이션 길이 가져오기 (선택적, 정확한 제어를 위해)
        AnimationClip[] clips = player.Animator.runtimeAnimatorController.animationClips;
        AnimationClip clip = System.Array.Find(clips, c => c.name == "MeleeAttack");
        if (clip != null)
        {
            attackAnimDuration = clip.length;
        }
        player.SetLastMeleeAttackTime(); // PlayerController에 쿨타임 기록 함수 호출
    }

    public override void Execute()
    {
        stateTimer += Time.deltaTime;

        // 특정 타이밍에 공격 판정 실행
        if (!damageDealt && stateTimer >= damageStartTime && stateTimer <= damageEndTime)
        {
            // PlayerController에 실제 공격 판정 로직을 위임 가능
            // player.PerformMeleeDamageCheck(player.Status.ATK.Value, ATTACK_RANGE, attackPoint, hittableLayers);
            Debug.Log($"근접 공격 데미지 : {player.Status.ATK.Value}"); // 실제 데미지 로직은 추후 구현
            damageDealt = true; // 한 번의 공격 상태에서 여러 번 데미지가 들어가지 않도록 (애니메이션에 따라 조절)
        }

        // 애니메이션 재생 완료 (또는 상태 지속 시간 완료) 후 Idle 상태로 전환
        if (stateTimer >= attackAnimDuration)
        {
            player.ChangeState(EPlayerState.Idle);
        }
    }

    public override void Exit()
    {
        
    }
}