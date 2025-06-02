using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIdleState : BossBaseState
{
    public BossIdleState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.Animator.Play("Idle"); 
        boss.Rb.velocity = Vector2.zero; // ����
    }

    public override void Execute()
    {
        // �ֿ켱 ����: Flying Skill
        if (boss.CurrentFlyingSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlyingAttack);
            return;
        }
        // ���� �켱 ����: Flame Skill
        if (boss.CurrentFlameSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlameAttack);
            return;
        }

        // �÷��̾� ����
        if (boss.GetPlayerDistance() <= boss.playerDetectionRange)
        {
            stateMachine.ChangeState(EBossState.Chase);
            return;
        }
        // �� �ܿ��� ��� Idle
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
        // �ֿ켱 ����: Flying Skill
        if (boss.CurrentFlyingSkillCooldown <= 0)
        {
            stateMachine.ChangeState(EBossState.FlyingAttack);
            return;
        }
        // ���� �켱 ����: Flame Skill
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

        // �Ϲ� ���� ��Ÿ���� �Ǿ��ٸ� ���� ���� ���·�
        if (boss.CurrentGeneralAttackCooldown <= 0)
        {
            // maintainDistanceRange���� ������ �ٷ� ���� ����, �ƴϸ� �� �ٰ�����
            if (distanceToPlayer <= boss.maintainDistanceRange || distanceToPlayer <= boss.rangedAttackDistanceThreshold) // ���� ���� �Ÿ�
            {
                stateMachine.ChangeState(EBossState.ChooseAttack);
                return;
            }
        }

        // �÷��̾�� �̵� (maintainDistanceRange �ٱ��� ���� ����)
        if (distanceToPlayer > boss.maintainDistanceRange)
        {
            Vector2 direction = boss.GetDirectionToPlayer();
            boss.Rb.velocity = direction * boss.moveSpeed;
        }
        else
        {
            boss.Rb.velocity = Vector2.zero; // �ʹ� ������ �ϴ� ���� (�Ǵ� ChooseAttack���� �ٷ� ��)
            // ���� maintainDistanceRange�� 0�̰�, �׻� ���� �������� �ٰ����ٸ� �� ������ ChooseAttack ������ ����
            if (boss.CurrentGeneralAttackCooldown <= 0) // ���� ��Ÿ�ӵ� �������� ����
            {
                stateMachine.ChangeState(EBossState.ChooseAttack);
                return;
            }
        }
    }

    public override void Exit()
    {
        boss.Rb.velocity = Vector2.zero; // ���� ���� �� ������ ����
    }
}

public class BossChooseAttackState : BossBaseState
{
    public BossChooseAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.Rb.velocity = Vector2.zero; // ���� ���� �߿��� ����

        // �÷��̾ ���ų� �ʹ� �־������� Idle (������ġ)
        if (boss.playerTransform == null || boss.GetPlayerDistance() > boss.playerDetectionRange)
        {
            stateMachine.ChangeState(EBossState.Idle);
            return;
        }

        // ���⼭ �ٷ� �Ÿ� �Ǵ� �� ���� ����
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
        // Enter���� �ٷ� ���¸� �����ϹǷ� Execute�� ����ΰų�,
        // ���� ���ÿ� �ð��� �ɸ��� �ִϸ��̼� ���� �ִٸ� ���⼭ ó��
    }

    public override void Exit() { }
}

public class BossRangedAttackState : BossBaseState
{
    private float attackAnimDuration = 1.5f; // ����: ���Ÿ� ���� �ִϸ��̼� ����, ���� ������ ����
    private float timer;
    private bool attackPerformed;

    public BossRangedAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine)
    {
        // �ִϸ��̼� ���̸� BossController�� AnimationClip���� ���� ���������� ���� ����
        // ��: attackAnimDuration = boss.GetAnimationLength("Attack_1");
    }

    public override void Enter()
    {
        timer = 0f;
        attackPerformed = false;
        boss.Rb.velocity = Vector2.zero; // ���� �߿��� ����
        boss.Animator.Play("RangeAttack"); 
    }

    public override void Execute()
    {
        timer += Time.deltaTime;

        // �ִϸ��̼� Ư�� ������ ���� ���� (�ִϸ��̼� �̺�Ʈ ��� ����)
        // ���⼭�� ������ �ð� ������� ó��
        if (!attackPerformed && timer >= 0.5f) // ��: 0.5�� �� �߻�
        {
            boss.PerformRangedAttack(); // �� �Լ� ������ ��Ÿ�� �ʱ�ȭ��
            attackPerformed = true;
        }

        if (timer >= attackAnimDuration) // �ִϸ��̼� ���� ��
        {
            stateMachine.ChangeState(EBossState.Idle); // �Ǵ� Chase
        }
    }

    public override void Exit() { }
}

public class BossMeleeAttackState : BossBaseState
{
    private float attackAnimDuration = 1f; // ���� ���� �ִϸ��̼� ����
    private float timer;
    private bool attackPerformed;
    // ���� ���� ���� Ÿ�̹� (�ִϸ��̼� ����)
    private float damageStartTime = 0.3f;
    private float damageEndTime = 0.6f;


    public BossMeleeAttackState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        timer = 0f;
        attackPerformed = false; // ���⼭�� ������ ������ �߻��ߴ��� ����
        boss.Rb.velocity = Vector2.zero;
        boss.Animator.Play("MeleeAttack"); // �ִϸ��̼� �̸� Ȯ��
    }

    public override void Execute()
    {
        timer += Time.deltaTime;

        // �ִϸ��̼��� Ư�� �������� ���� ����
        if (!attackPerformed && timer >= damageStartTime && timer <= damageEndTime)
        {
            // ���� ������ ���� ���� (BossController�� ���� ����)
            // Debug.Log("Melee Attack Hit Check Window Active!");
            // �� �������� �÷��̾ ���� ���� �ְ�, ���� �� �������� �������� ���� �ʾҴٸ� ������ ó��
            // �ѹ��� ������ ������ attackPerformed �÷��� ��� ����
        }

        if (timer > damageEndTime && !attackPerformed) // ������ ������ ������ ���� ���� ��������
        {
            // �� �������� ������ �� �� (������ ��Ÿ���� ���ƾ� ��)
            boss.CurrentGeneralAttackCooldown = boss.generalAttackCooldown; // ���� �õ��� �����Ƿ� ��Ÿ��
            attackPerformed = true; // �� ���� ���� ���¿��� ���� �� ��Ÿ�� ���� �ʵ���
        }


        if (timer >= attackAnimDuration)
        {
            if (!attackPerformed) // ���� ������ �� ���� �� �Ͼ���� ��Ÿ�� ����
            {
                boss.CurrentGeneralAttackCooldown = boss.generalAttackCooldown;
            }
            stateMachine.ChangeState(EBossState.Idle); // �Ǵ� Chase
        }
    }

    public override void Exit() { }
}

public class BossFlameSkillState : BossBaseState
{
    private float skillAnimDuration = 2f; // ����: ��� ��ų ���� �ִϸ��̼� ����
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

        // �ִϸ��̼� Ư�� ������ ��ų ����
        if (!skillPerformed && timer >= 1.0f) // ��: 1�� �� ��� ����
        {
            boss.PerformFlameSkill(); // �� �Լ� ������ ��Ÿ�� �ʱ�ȭ��
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
    // ���� �� ���� ������ BossController�� �ڷ�ƾ���� ��κ� ó����
    // �� ���´� �ش� �ڷ�ƾ�� ����/�����ϰ�, �ٸ� ���·��� ��ȯ�� ����

    public BossFlyingSkillState(BossController boss, BossStateMachine stateMachine) : base(boss, stateMachine) { }

    public override void Enter()
    {
        boss.StartFlyingSequence(); // BossController�� �ڷ�ƾ ����
    }

    public override void Execute()
    {
        // BossController�� FlyingSkillRoutine �ڷ�ƾ�� ��κ��� ������ ó��.
        // �ڷ�ƾ�� �Ϸ�Ǹ� (�Ǵ� Ư�� ���ǿ� ���� �ߴܵǸ�) BossController ���ο���
        // Idle ���� ������ ��ȯ�ϵ��� ������ ���� �ְ�,
        // ���⼭ BossController�� Ư�� �÷��׸� Ȯ���Ͽ� ��ȯ�� ���� ����.
        // ���� BossController.FlyingSkillRoutine �������� Idle�� ��ȯ�ϵ��� �Ǿ� ����.
        // ���� �� Execute�� ����ְų�, ���� �� ��� ���� ���� üũ�� �� ����.

        // ����: ���� ���� �� Ư�� �������� ����ؾ� �Ѵٸ�
        // if (some_cancel_condition)
        // {
        //     boss.StopFlyingSequence(); // �ڷ�ƾ �ߴ� �� ��ó��
        //     stateMachine.ChangeState(EBossState.Idle);
        // }
    }

    public override void Exit()
    {
        // ���� Enter���� ������ �ڷ�ƾ�� Exit �������� ������ �ʾҴµ� ������ ���°� �ٲ�ٸ�
        // ���⼭ boss.StopFlyingSequence()�� ȣ���Ͽ� ������ �� ����.
        // ������ �Ϲ������δ� �ڷ�ƾ�� ������ �Ϸ�ǰ� ���¸� ��ȯ�ϰų�,
        // �ڷ�ƾ ������ ���� ���¸� �����ϴ� ���� �� �����ϱ� ����.
        // ����� �ڷ�ƾ�� ������ BossController���� Idle�� ��.
    }
}
