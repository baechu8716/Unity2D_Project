using System.Linq;
using UnityEngine;



public class IdleState : BaseState
{
    public IdleState(PlayerController player) : base(player) { } 

    public override void Enter()
    {
        player.Animator.Play("Idle"); 
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
    public override void Enter() { player.Animator.Play("Run"); } 
    public override void Execute()
    {
        float moveInput = Input.GetAxisRaw("Horizontal"); 
        player.Movement.Move(moveInput); 

        if (moveInput == 0) player.ChangeState(EPlayerState.Idle); 
        if (Input.GetButtonDown("Jump") && player.Movement.IsGrounded()) player.ChangeState(EPlayerState.Jump); 
        if (Input.GetKeyDown(KeyCode.LeftShift) && player.CanRoll()) player.ChangeState(EPlayerState.Roll); 
        if (!player.Movement.IsGrounded() && player.Movement.VerticalVelocity < 0) player.ChangeState(EPlayerState.Fall); 
        if (Input.GetMouseButtonDown(1)) player.ChangeState(EPlayerState.Attack); 
        if (Input.GetMouseButtonDown(0) && player.CanMeleeAttack()) player.ChangeState(EPlayerState.MeleeAttack); 
    }
    public override void Exit() { }
}

public class JumpState : BaseState
{
    private float jumpTime; 
    private readonly float minJumpAnimTime = 0.3f; 

    public JumpState(PlayerController player) : base(player) { } 

    public override void Enter()
    {
        player.Movement.Jump(); 
        jumpTime = 0f; 
        player.Animator.Play("Jump", 0, 0f); 
        player.HasPlayedJumpAnimation = true; 
    }

    public override void Execute()
    {
        jumpTime += Time.deltaTime; //
        float moveInput = Input.GetAxisRaw("Horizontal"); 
        player.Movement.Move(moveInput); //

        if (Input.GetMouseButtonDown(1)) player.ChangeState(EPlayerState.Attack); 

        // 기존 FallState 전환 로직 유지 또는 단순화
        if (player.Movement.VerticalVelocity < -0.1f && !player.Movement.IsGrounded()) // 임계값 사용
        {
            if (jumpTime >= minJumpAnimTime) // 최소 점프 애니메이션 시간 이후에만 Fall로 전환
                player.ChangeState(EPlayerState.Fall); 
        }


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
    public override void Enter() { player.Animator.Play("Fall", 0, 0f); } 
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
    private float rollDuration = 0.5f; // 구르기 지속시간
    private float rollTimer; 

    public RollState(PlayerController player) : base(player) { } 

    public override void Enter()
    {
        // CanRoll은 Idle/Move에서 이미 체크하고 오므로 여기선 생략 가능, 혹은 이중 체크
        player.Animator.Play("Roll", 0, 0f); 
        player.Movement.Roll(); // 실제 이동 로직 호출
        player.PerformRoll(); // PlayerController의 쿨타임 관리 메서드 호출 (기존 OnRoll 대체)
        player.SetInvincibility(true); // 무적 설정
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
        player.SetInvincibility(false); // 무적 해제
    }

}

public class AttackState : BaseState
{
    private float attackTimer = 0f; 
    private bool waitingForFire = false; 
    private bool arrowFired = false; 

    private AnimationClip attackClip; 
    private float frameRate; 
    private float drawFrameTime;  // 활 당기기 완료 시간 (초) - 애니메이션 특정 프레임 기준
    private float totalAnimTime;  // 전체 Attack 애니메이션 시간 (초)
    private float normalizedDrawTime; // 정규화된 당기기 완료 시간


    public AttackState(PlayerController player) : base(player) { } 

    public override void Enter()
    {
        attackTimer = 0f; 
        waitingForFire = false; 
        arrowFired = false; 

        player.Animator.Play("Attack", 0, 0f);
        player.Animator.speed = 1f; // 애니메이션 속도 정상화

        // 애니메이션 클립 정보 가져오기
        attackClip = player.Animator.runtimeAnimatorController.animationClips
                        .FirstOrDefault(c => c.name == "Attack"); // "Attack" 클립 찾기

        if (attackClip != null)
        {
            frameRate = attackClip.frameRate > 0 ? attackClip.frameRate : 60f; // 프레임 속도
            // drawFrameTime은 애니메이션의 특정 프레임(예: 40프레임)을 시간으로 변환
            drawFrameTime = 40f / frameRate; // 예시: 40프레임에서 멈춤
            totalAnimTime = attackClip.length; // 전체 애니메이션 길이
            normalizedDrawTime = totalAnimTime > 0 ? drawFrameTime / totalAnimTime : 0.5f; // 정규화된 시간 계산
        }
        else
        {
            // Attack 클립을 찾지 못한 경우 기본값 설정 또는 에러 처리
            frameRate = 60f; 
            drawFrameTime = 40f / frameRate; 
            totalAnimTime = 1f; 
            normalizedDrawTime = drawFrameTime / totalAnimTime; 
        }
        player.SetCameraZoom(player.zoomInSize, player.zoomTransitionDuration);
    }

    public override void Execute()
    {
        attackTimer += Time.deltaTime; 
        AnimatorStateInfo currentStateInfo = player.Animator.GetCurrentAnimatorStateInfo(0); // 현재 애니메이터 상태 정보

        if (!currentStateInfo.IsName("Attack")) return; // Attack 애니메이션이 아니면 실행 중지

        float currentNormalizedTime = currentStateInfo.normalizedTime % 1; // 애니메이션 루프 고려 (현재는 루프 안 함)

        // 1. 활 당기기 (애니메이션 정지 전)
        if (!arrowFired && !waitingForFire)
        {
            if (currentNormalizedTime >= normalizedDrawTime) // 당기기 시간 도달 시
            {
                player.Animator.speed = 0f; // 애니메이션 정지
                waitingForFire = true; // 발사 대기 상태로
            }
            else if (Input.GetMouseButtonUp(1)) // 당기는 중 우클릭 떼면 취소
            {
                player.Animator.speed = 1f; 
                player.ChangeState(EPlayerState.Idle); 
                return;
            }
        }

        // 2. 발사 대기 (애니메이션 정지, 발사 전)
        if (waitingForFire && !arrowFired)
        {
            if (Input.GetMouseButtonDown(0)) // 좌클릭으로 발사
            {
                player.FireArrow(); // 화살 발사
                SFXManager.Instance.PlayPlayerAttackSound();
                player.Animator.speed = 1f; // 애니메이션 다시 재생
                arrowFired = true; // 발사됨 표시
                waitingForFire = false; // 대기 상태 해제
            }
            else if (Input.GetMouseButtonUp(1)) // 대기 중 우클릭 떼면 취소
            {
                player.Animator.speed = 1f; 
                player.ChangeState(EPlayerState.Idle); 
                return;
            }
        }

        // 3. 발사 후 (애니메이션 완료 대기 또는 연속 공격)
        if (arrowFired)
        {
            if (currentNormalizedTime >= 0.98f) // 애니메이션 거의 끝 (1.0f로 비교 시 미세오차 가능성)
            {
                if (Input.GetMouseButton(1)) // 우클릭 유지 시 연속 공격 (재조준)
                {
                    // 상태 재진입처럼 동작하도록 초기화
                    Enter(); // 상태 변수 재설정 및 애니메이션 처음부터 재생
                }
                else // 우클릭 떼면 Idle로
                {
                    player.ChangeState(EPlayerState.Idle); 
                }
            }
        }
    }

    public override void Exit()
    {
        player.Animator.speed = 1f; // 상태 종료 시 애니메이션 속도 복원
        // AttackState 종료 시 카메라 줌 아웃
        player.SetCameraZoom(player.zoomOutSize, player.zoomTransitionDuration);
    }

    public bool IsWaitingForFire()
    {
        return waitingForFire; // 외부에서 조준 상태 확인용
    }
}

public class MeleeAttackState : BaseState
{
    private float attackAnimDuration; 
    private float stateTimer; 
    private bool damageDealt; 

    private float damageStartTime = 0.1f; // 데미지 판정 시작 시간 (애니메이션 기준)
    private float damageEndTime = 0.4f;   // 데미지 판정 종료 시간 (애니메이션 기준)

    // PlayerController에서 받아올 값들
    private float attackRange; 
    private Transform attackPoint; 
    private LayerMask hittableLayers; 

    public MeleeAttackState(PlayerController player) : base(player)
    {
        this.attackRange = player.meleeAttackRange;
        this.attackPoint = player.meleeAttackPoint; 
        this.hittableLayers = player.enemyLayers;   
    }

    public override void Enter()
    {
        stateTimer = 0f; 
        damageDealt = false; 
        player.Animator.Play("MeleeAttack"); // "MeleeAttack" 애니메이션 재생

        // 애니메이션 길이 설정 (정확한 제어를 위해)
        AnimationClip clip = player.Animator.runtimeAnimatorController.animationClips
                            .FirstOrDefault(c => c.name == "MeleeAttack"); // "MeleeAttack" 클립 찾기
        if (clip != null)
        {
            attackAnimDuration = clip.length; 
        }
        else
        {
            attackAnimDuration = 0.5f; 
        }
        player.PerformMeleeAttack();
    }

    public override void Execute()
    {
        stateTimer += Time.deltaTime; 

        // 데미지 판정 시간
        if (!damageDealt && stateTimer >= damageStartTime && stateTimer <= damageEndTime) 
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, hittableLayers); // 공격 판정
            foreach (var hit in hits)
            {
                IDamageable damageableTarget = hit.GetComponent<IDamageable>();
                if (damageableTarget != null)
                {
                    damageableTarget.TakeDamage(player.Status.ATK.Value); // 플레이어 공격력으로 데미지
                    Debug.Log($"Player dealt {player.Status.ATK.Value} melee damage to {hit.gameObject.name}");
                    damageDealt = true; // 이번 공격 동안 한 번만 데미지 (광역기가 아니라면)
                    // break; // 단일 타겟만 원하면 주석 해제
                }
            }
            // 데미지 판정은 한 번만 실행되도록 damageDealt 플래그를 여기서 true로 설정 가능
            // damageDealt = true; // 루프 후 설정하면 여러 대상 타격 가능
        }

        if (stateTimer >= attackAnimDuration) // 애니메이션(상태) 종료
        {
            player.ChangeState(EPlayerState.Idle); 
        }
    }
    public override void Exit() { }
}


public class HitState : BaseState
{
    public HitState(PlayerController player) : base(player) { } 

    public override void Enter()
    {
        player.Animator.Play("Hit"); 
        player.Movement.StopImmediately(); // 현재 움직임을 즉시 멈춤
        player.movementDisabled = true;   // 플레이어 입력을 비활성화
    }

    public override void Execute()
    {
        // "Hit" 애니메이션이 끝나면 Idle 상태로 돌아감
        AnimatorStateInfo stateInfo = player.Animator.GetCurrentAnimatorStateInfo(0); 
        if (stateInfo.IsName("Hit") && stateInfo.normalizedTime >= 1.0f) 
        {
            player.ChangeState(EPlayerState.Idle); 
        }
    }

    public override void Exit()
    {
        player.movementDisabled = false; // 플레이어 입력을 다시 활성화
    }
}



public class PlayerDieState : BaseState
{
    public PlayerDieState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play("Die"); 
        player.Movement.StopImmediately(); // 모든 움직임 정지
        player.movementDisabled = true;   // 이동 및 모든 관련 입력 처리(방향 전환 포함) 비활성화
        Debug.Log("Player entered DieState. All inputs disabled."); //
    }

    public override void Execute()
    {

    }

    public override void Exit()
    {

    }
}