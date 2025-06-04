using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BossIdleState : BossBaseState
{
    public BossIdleState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { } //

    public override void Enter()
    {
        boss.Animator.Play("Idle"); 
        boss.Rb.velocity = Vector2.zero; 
    }

    public override void Execute()
    {
        if (boss.CurrentFlyingSkillCooldown <= 0) 
        {
            stateMachine.ChangeState(EBossState.FlyingAttack); 
            return;
        }
        if (boss.CurrentFlameSkillCooldown <= 0) 
        {
            stateMachine.ChangeState(EBossState.FlameAttack); 
            return;
        }
        if (boss.GetPlayerDistance() <= boss.playerDetectionRange) 
        {
            stateMachine.ChangeState(EBossState.Chase);
            return;
        }
    }
    public override void Exit() { }
}

public class BossChaseState : BossBaseState
{
    public BossChaseState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { } 

    public override void Enter()
    {
        boss.Animator.Play("Walk"); 
    }

    public override void Execute()
    {
        float distanceToPlayer = boss.GetPlayerDistance();

        if (distanceToPlayer > boss.playerDetectionRange)
        {
            stateMachine.ChangeState(EBossState.Idle);
            return;
        }

        if (boss.CurrentFlyingSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlyingAttack);
            return;
        }

        if (boss.CurrentFlameSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlameAttack);
            return;
        }

        if (boss.CurrentGeneralAttackCooldown <= 0 &&
            distanceToPlayer <= boss.rangedAttackDistanceThreshold)
        {
            stateMachine.ChangeState(EBossState.ChooseAttack);
            return;
        }

        if (distanceToPlayer > boss.maintainDistanceRange)
        {
            Vector2 direction = boss.GetDirectionToPlayer();
            boss.Rb.velocity = direction * boss.moveSpeed;

            // 움직이기 시작하면 Walk 애니메이션 재생
            if (!boss.Animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"))
                boss.Animator.Play("Walk");
        }
        else
        {
            boss.Rb.velocity = Vector2.zero;

            // 가까이 있어 멈추면 Idle 애니메이션 재생
            if (!boss.Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                boss.Animator.Play("Idle");
        }
    }

    public override void Exit() { boss.Rb.velocity = Vector2.zero; } 
}


public class BossChooseAttackState : BossBaseState
{
    public BossChooseAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { } 

    public override void Enter()
    {
        boss.Rb.velocity = Vector2.zero; 

        if (boss.playerTransform == null || boss.GetPlayerDistance() > boss.playerDetectionRange) 
        {
            stateMachine.ChangeState(EBossState.Idle); 
            return;
        }

        float distanceToPlayer = boss.GetPlayerDistance(); 

        // 이 상태에 진입했다면 rangedAttackDistanceThreshold 안에 있는 것으로 가정 (ChaseState에서 판단)
        if (distanceToPlayer > boss.maintainDistanceRange) // 노란 원 밖 (하지만 빨간 원 안) -> 원거리
        {
            stateMachine.ChangeState(EBossState.RangedAttack); 
        }
        else // 노란 원 안 -> 근접
        {
            stateMachine.ChangeState(EBossState.MeleeAttack); 
        }
    }
    public override void Execute() { }
    public override void Exit() { }
}


public class BossRangedAttackState : BossBaseState
{
    private float attackAnimDuration = 1.5f; 
    private float timer; 
    private bool attackActionTriggered; 

    public BossRangedAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { } 

    public override void Enter()
    {
        boss.Animator.Play("RangeAttack");
        SFXManager.Instance.PlayBossAttackSound();
        timer = 0f; 
        attackActionTriggered = false; 
        boss.Rb.velocity = Vector2.zero; 
    }

    public override void Execute()
    {
        timer += Time.deltaTime; 

        if (!attackActionTriggered && timer >= 0.5f) // 예: 애니메이션 0.5초 시점에 발사
        {
            boss.PerformRangedAttackAction(); // 수정된 메서드 이름 사용
            attackActionTriggered = true; 
        }

        if (timer >= attackAnimDuration) 
        {
            stateMachine.ChangeState(EBossState.Idle); 
        }
    }
    public override void Exit() { }
}

public class BossMeleeAttackState : BossBaseState
{
    private float attackAnimDuration = 1f; 
    private float timer; 
    private bool damageDealtThisAttack; 

    private float damageWindowStartTime = 0.3f; 
    private float damageWindowEndTime = 0.6f; 
    // meleeAttackRange는 BossController에서 직접 가져오거나, BossController의 meleeAttackPoint 주변을 탐색
    private float meleeAttackRange; // 이 값은 BossController에서 설정된 값을 가져와야 함


    public BossMeleeAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
        this.meleeAttackRange = boss.maintainDistanceRange + 0.5f; 
    }

    public override void Enter()
    {
        boss.Animator.Play("MeleeAttack"); 
        timer = 0f; 
        damageDealtThisAttack = false; 
        boss.Rb.velocity = Vector2.zero; 
    }

    public override void Execute()
    {
        timer += Time.deltaTime; 

        if (!damageDealtThisAttack && timer >= damageWindowStartTime && timer <= damageWindowEndTime) 
        {
            Transform attackPoint = boss.meleeAttackPoint != null ? boss.meleeAttackPoint : boss.transform;
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, this.meleeAttackRange, LayerMask.GetMask("Player")); // "Player" 레이어의 적을 탐색

            foreach (var hit in hits)
            {
                IDamageable playerDamageable = hit.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    playerDamageable.TakeDamage(boss.Status.ATK); // 보스 공격력으로 데미지
                    damageDealtThisAttack = true; 
                    break;
                }
            }
            if (damageDealtThisAttack)
            {
                boss.PerformMeleeAttackAction(); // 넉백 및 쿨다운 시작 (데미지 성공 시)
            }
        }

        if (timer >= attackAnimDuration) 
        {
            if (!damageDealtThisAttack) // 공격 못 맞췄어도 쿨타임 시작
            {
                boss.CurrentGeneralAttackCooldown = boss.generalAttackCooldown; 
            }
            stateMachine.ChangeState(EBossState.Idle); 
        }
    }
    public override void Exit() { }
}


public class BossFlameSkillState : BossBaseState
{
    private float skillAnimDuration = 2f; 
    private float timer; 
    private bool skillPerformed; 

    public BossFlameSkillState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { } 

    public override void Enter()
    {
        boss.Animator.Play("Idle"); 
        timer = 0f; 
        skillPerformed = false; 
        boss.Rb.velocity = Vector2.zero; 
    }

    public override void Execute()
    {
        timer += Time.deltaTime; 
        if (!skillPerformed && timer >= 1.0f) // 예: 1초 뒤 스킬 발동
        {
            boss.PerformFlameSkillAction(); 
            skillPerformed = true; 
        }
        if (timer >= skillAnimDuration) 
        {
            stateMachine.ChangeState(EBossState.Idle); 
        }
    }
    public override void Exit() { }
}

public class BossFlyingSkillState : BossBaseState
{
    public BossFlyingSkillState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { } //

    public override void Enter()
    {
        boss.Animator.Play("Fly"); // 비행 애니메이션
        boss.StartFlyingSequence(); // 비행 시퀀스 시작
    }

    public override void Execute()
    {
        // 비행 로직은 BossController의 코루틴에서 주로 처리됨
        // 여기서 IsFlying() 등을 통해 코루틴이 끝났는지 확인하고 Idle로 전환할 수도 있지만,
        // 현재는 코루틴 내부에서 직접 상태를 전환하고 있음. (BossController.FlyingSkillRoutine)
        if (!boss.IsFlying() && boss.StateMachine.CurrentState == this) // 코루틴이 끝났는데 아직 이 상태라면 Idle로
        {
            stateMachine.ChangeState(EBossState.Idle);
        }
    }
    public override void Exit()
    {
        // 만약 비행 중에 강제로 다른 상태로 전환된다면, 비행 중단 처리
        if (boss.IsFlying())
        {
            boss.StopFlyingSequenceAndLand(); // 강제 착지
        }
    }
}
public class BossHitState : BossBaseState
{
    private float hitAnimationDuration;
    private float stateTimer;

    public BossHitState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.Animator.Play("Hit");
        boss.Rb.velocity = Vector2.zero; // 피격 시 잠시 멈춤
        stateTimer = 0f;

        // "Hit" 애니메이션 클립의 길이 가져오기
        AnimationClip hitClip = boss.Animator.runtimeAnimatorController.animationClips
                                .FirstOrDefault(clip => clip.name == "Hit");
        if (hitClip != null)
        {
            hitAnimationDuration = hitClip.length;
        }
        else
        {
            hitAnimationDuration = 0.5f; // 기본 지속 시간 (애니메이션 클립 못 찾을 시)
        }
    }

    public override void Execute()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= hitAnimationDuration)
        {
            if (boss.GetPlayerDistance() <= boss.playerDetectionRange)
            {
                stateMachine.ChangeState(EBossState.Chase);
            }
            else
            {
                stateMachine.ChangeState(EBossState.Idle);
            }
        }
    }

    public override void Exit() { }
}
public class BossDieState : BossBaseState
{
    public BossDieState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.Animator.Play("Die"); // "Die" 애니메이션 재생

        // 보스의 모든 행동 중지
        if (boss.Rb != null)
        {
            boss.Rb.velocity = Vector2.zero;
            boss.Rb.isKinematic = true; // 물리적 움직임 완전히 중단
        }
        Collider2D bossCollider = boss.GetComponent<Collider2D>();
        if (bossCollider != null)
        {
            bossCollider.enabled = false; // 더 이상 충돌하지 않도록
        }
        boss.HandleDeathEffectsAndCleanup(); 
    }

    public override void Execute()
    {
    }

    public override void Exit()
    {

    }
}
