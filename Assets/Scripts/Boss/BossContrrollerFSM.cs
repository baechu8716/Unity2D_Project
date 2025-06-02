using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIdleState : BossBaseState
{
    public BossIdleState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.Animator.Play("Idle"); 
        boss.Rb.velocity = Vector2.zero; // 정지
    }

    public override void Execute()
    {
        // 최우선 순위: Flying Skill
        if (boss.CurrentFlyingSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlyingAttack);
            return;
        }
        // 다음 우선 순위: Flame Skill
        if (boss.CurrentFlameSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlameAttack);
            return;
        }

        // 플레이어 감지
        if (boss.GetPlayerDistance() <= boss.playerDetectionRange)
        {
            stateMachine.ChangeState(EBossState.Chase);
            return;
        }
        // 그 외에는 계속 Idle
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
        // 최우선 순위: Flying Skill
        if (boss.CurrentFlyingSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlyingAttack);
            return;
        }
        // 다음 우선 순위: Flame Skill
        if (boss.CurrentFlameSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlameAttack);
            return;
        }

        float distanceToPlayer = boss.GetPlayerDistance();

        if (distanceToPlayer > boss.playerDetectionRange)
        {
            stateMachine.ChangeState(EBossState.Idle);
            return;
        }

        // 일반 공격 쿨타임이 되었다면 공격 선택 상태로
        if (boss.CurrentGeneralAttackCooldown <= 0)
        {
            // maintainDistanceRange보다 가까우면 바로 공격 선택, 아니면 더 다가가기
            if (distanceToPlayer <= boss.maintainDistanceRange || distanceToPlayer <= boss.rangedAttackDistanceThreshold) // 공격 가능 거리
            {
                stateMachine.ChangeState(EBossState.ChooseAttack);
                return;
            }
        }

        // 플레이어에게 이동 (maintainDistanceRange 바깥에 있을 때만)
        if (distanceToPlayer > boss.maintainDistanceRange)
        {
            Vector2 direction = boss.GetDirectionToPlayer();
            boss.Rb.velocity = direction * boss.moveSpeed;
        }
        else
        {
            boss.Rb.velocity = Vector2.zero; // 너무 가까우면 일단 멈춤 (또는 ChooseAttack으로 바로 감)
            // 만약 maintainDistanceRange가 0이고, 항상 공격 범위까지 다가간다면 이 조건은 ChooseAttack 결정에 포함
            if (boss.CurrentGeneralAttackCooldown <= 0) // 가까운데 쿨타임도 돌았으면 공격
            {
                stateMachine.ChangeState(EBossState.ChooseAttack);
                return;
            }
        }
    }

    public override void Exit()
    {
        boss.Rb.velocity = Vector2.zero; // 상태 종료 시 움직임 정지
    }
}

public class BossChooseAttackState : BossBaseState
{
    public BossChooseAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.Rb.velocity = Vector2.zero; // 공격 선택 중에는 멈춤

        // 플레이어가 없거나 너무 멀어졌으면 Idle (안전장치)
        if (boss.playerTransform == null || boss.GetPlayerDistance() > boss.playerDetectionRange)
        {
            stateMachine.ChangeState(EBossState.Idle);
            return;
        }

        // 여기서 바로 거리 판단 후 상태 변경
        if (boss.GetPlayerDistance() <= boss.rangedAttackDistanceThreshold)
        {
            stateMachine.ChangeState(EBossState.RangedAttack);
        }
        else
        {
            stateMachine.ChangeState(EBossState.MeleeAttack);
        }
    }

    public override void Execute()
    {
        // Enter에서 바로 상태를 변경하므로 Execute는 비워두거나,
        // 만약 선택에 시간이 걸리는 애니메이션 등이 있다면 여기서 처리
    }

    public override void Exit() { }
}

public class BossRangedAttackState : BossBaseState
{
    private float attackAnimDuration = 1.5f; // 예시: 원거리 공격 애니메이션 길이, 실제 값으로 변경
    private float timer;
    private bool attackPerformed;

    public BossRangedAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
        // 애니메이션 길이를 BossController나 AnimationClip에서 직접 가져오도록 수정 가능
        // 예: attackAnimDuration = boss.GetAnimationLength("Attack_1");
    }

    public override void Enter()
    {
        timer = 0f;
        attackPerformed = false;
        boss.Rb.velocity = Vector2.zero; // 공격 중에는 멈춤
        boss.Animator.Play("RangeAttack"); 
    }

    public override void Execute()
    {
        timer += Time.deltaTime;

        // 애니메이션 특정 시점에 공격 실행 (애니메이션 이벤트 사용 권장)
        // 여기서는 간단히 시간 기반으로 처리
        if (!attackPerformed && timer >= 0.5f) // 예: 0.5초 뒤 발사
        {
            boss.PerformRangedAttack(); // 이 함수 내에서 쿨타임 초기화됨
            attackPerformed = true;
        }

        if (timer >= attackAnimDuration) // 애니메이션 종료 후
        {
            stateMachine.ChangeState(EBossState.Idle); // 또는 Chase
        }
    }

    public override void Exit() { }
}

public class BossMeleeAttackState : BossBaseState
{
    private float attackAnimDuration = 1f; // 근접 공격 애니메이션 길이
    private float timer;
    private bool attackPerformed;
    // 근접 공격 판정 타이밍 (애니메이션 기준)
    private float damageStartTime = 0.3f;
    private float damageEndTime = 0.6f;


    public BossMeleeAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        timer = 0f;
        attackPerformed = false; // 여기서는 데미지 판정이 발생했는지 여부
        boss.Rb.velocity = Vector2.zero;
        boss.Animator.Play("MeleeAttack"); // 애니메이션 이름 확인
    }

    public override void Execute()
    {
        timer += Time.deltaTime;

        // 애니메이션의 특정 구간에서 공격 판정
        if (!attackPerformed && timer >= damageStartTime && timer <= damageEndTime)
        {
            // 실제 데미지 판정 로직 (BossController에 위임 가능)
            // Debug.Log("Melee Attack Hit Check Window Active!");
            // 이 구간에서 플레이어가 범위 내에 있고, 아직 이 공격으로 데미지를 입지 않았다면 데미지 처리
            // 한번만 데미지 들어가도록 attackPerformed 플래그 사용 가능
        }

        if (timer > damageEndTime && !attackPerformed) // 데미지 구간이 지났고 공격 실행 안했으면
        {
            // 이 공격으로 데미지 못 줌 (하지만 쿨타임은 돌아야 함)
            boss.CurrentGeneralAttackCooldown = boss.generalAttackCooldown; // 공격 시도는 했으므로 쿨타임
            attackPerformed = true; // 한 번의 공격 상태에서 여러 번 쿨타임 돌지 않도록
        }


        if (timer >= attackAnimDuration)
        {
            if (!attackPerformed) // 공격 판정이 한 번도 안 일어났으면 쿨타임 설정
            {
                boss.CurrentGeneralAttackCooldown = boss.generalAttackCooldown;
            }
            stateMachine.ChangeState(EBossState.Idle); // 또는 Chase
        }
    }

    public override void Exit() { }
}

public class BossFlameSkillState : BossBaseState
{
    private float skillAnimDuration = 2f; // 예시: 기둥 스킬 시전 애니메이션 길이
    private float timer;
    private bool skillPerformed;

    public BossFlameSkillState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        timer = 0f;
        skillPerformed = false;
        boss.Rb.velocity = Vector2.zero;
        boss.Animator.Play("Idle"); 
    }

    public override void Execute()
    {
        timer += Time.deltaTime;

        // 애니메이션 특정 시점에 스킬 실행
        if (!skillPerformed && timer >= 1.0f) // 예: 1초 뒤 기둥 생성
        {
            boss.PerformFlameSkill(); // 이 함수 내에서 쿨타임 초기화됨
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
    // 비행 및 공격 로직은 BossController의 코루틴에서 대부분 처리됨
    // 이 상태는 해당 코루틴을 시작/종료하고, 다른 상태로의 전환을 관리

    public BossFlyingSkillState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.StartFlyingSequence(); // BossController의 코루틴 시작
    }

    public override void Execute()
    {
        // BossController의 FlyingSkillRoutine 코루틴이 대부분의 로직을 처리.
        // 코루틴이 완료되면 (또는 특정 조건에 의해 중단되면) BossController 내부에서
        // Idle 상태 등으로 전환하도록 설계할 수도 있고,
        // 여기서 BossController의 특정 플래그를 확인하여 전환할 수도 있음.
        // 현재 BossController.FlyingSkillRoutine 마지막에 Idle로 전환하도록 되어 있음.
        // 따라서 이 Execute는 비어있거나, 비행 중 취소 조건 등을 체크할 수 있음.

        // 예시: 만약 비행 중 특정 조건으로 취소해야 한다면
        // if (some_cancel_condition)
        // {
        //     boss.StopFlyingSequence(); // 코루틴 중단 및 후처리
        //     stateMachine.ChangeState(EBossState.Idle);
        // }
    }

    public override void Exit()
    {
        // 만약 Enter에서 시작한 코루틴이 Exit 시점까지 끝나지 않았는데 강제로 상태가 바뀐다면
        // 여기서 boss.StopFlyingSequence()를 호출하여 정리할 수 있음.
        // 하지만 일반적으로는 코루틴이 스스로 완료되고 상태를 전환하거나,
        // 코루틴 내에서 다음 상태를 결정하는 것이 더 관리하기 편함.
        // 현재는 코루틴이 끝나면 BossController에서 Idle로 감.
    }
}
