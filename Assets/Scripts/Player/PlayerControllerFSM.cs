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
    private readonly float minJumpAnimTime = 0.3f; // ������ �ð����� ����
    private float previousYVelocity; // ���� �������� yVelocity

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

        // ���� �ð� ���Ŀ��� ������ üũ
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
    private bool waitingForFire = false; // Ȱ�� ���� �߻縦 ��ٸ��� ����
    private bool arrowFired = false;     // ȭ���� �߻�Ǿ����� ����

    private AnimationClip attackClip;
    private float frameRate;
    private float drawFrameTime;      // Ȱ ���� �Ϸ� �ð� (��)
    private float totalAnimTime;      // ��ü Attack �ִϸ��̼� �ð� (��)
    private float normalizedDrawTime; // ����ȭ�� Ȱ ���� �Ϸ� �ð�

    public AttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        attackTimer = 0f;
        waitingForFire = false;
        arrowFired = false;

        player.Animator.Play("Attack", 0, 0f); // �ִϸ��̼��� ó������ ���
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
            // ���� AttackState������, �ִϸ����Ͱ� Attack �ִϸ��̼��� ����ϰ� ���� �ʴٸ�
            // (��: �ſ� ª�� ��ȯ �߿� �ٸ� �ִϸ��̼��� ��� ����� ���ɼ�)
            // �ϴ��� �ƹ��͵� �� �ϰų�, Idle�� ����
            return;
        }

        float currentNormalizedTime = currentStateInfo.normalizedTime;

        // 1. Ȱ ���� (���� �߻� ��, ���� ��� ���µ� �ƴ�)
        if (!arrowFired && !waitingForFire)
        {
            if (currentNormalizedTime >= normalizedDrawTime)
            {
                player.Animator.speed = 0f; // ��ǥ �����ӿ��� �ִϸ��̼� ����
                waitingForFire = true;     // �߻� ��� ���·� ����
            }
            // Ȱ ���� �� ��Ŭ�� ���� ���� ���
            else if (Input.GetMouseButtonUp(1))
            {
                player.Animator.speed = 1f;
                player.ChangeState(EPlayerState.Idle);
                return;
            }
        }

        // 2. �߻� ��� �� (�ִϸ��̼� �����ְ�, ���� �߻� ��)
        if (waitingForFire && !arrowFired)
        {
            // ��Ŭ������ �߻�
            if (Input.GetMouseButtonDown(0))
            {
                player.FireArrow();          // ȭ�� �߻�
                player.Animator.speed = 1f;  // ������ �ִϸ��̼� ���
                arrowFired = true;          // ȭ�� �߻�� ǥ��
                waitingForFire = false;     // ���� �߻� ��� ���°� �ƴ�
            }
            // �߻� ��� �� ��Ŭ�� ���� ���� ���
            else if (Input.GetMouseButtonUp(1))
            {
                player.Animator.speed = 1f;
                player.ChangeState(EPlayerState.Idle);
                return;
            }
        }

        // 3. ȭ�� �߻� �� (�ִϸ��̼� �Ϸ� �Ǵ� ���� ������ ���� ������)
        if (arrowFired)
        {
            // �ִϸ��̼��� ������ �����ٸ� (normalizedTime >= 1.0f)
            if (currentNormalizedTime >= 1.0f)
            {
                if (Input.GetMouseButton(1)) // ��Ŭ���� ������ �����ִٸ� (���� ����)
                {
                    // ���¸� �ʱ�ȭ�Ͽ� �ٽ� Ȱ ������� ����
                    attackTimer = 0f;
                    waitingForFire = false; // �� ���� ���� �������� ù ��° if ��Ͽ��� true�� ������ ����
                    arrowFired = false;
                    player.Animator.Play("Attack", 0, 0f); // �ִϸ��̼��� ó������ �ٽ�
                    player.Animator.speed = 1f;
                    // �ʿ��� ������ �����ϰ� �ִϸ��̼��� �ٽ� ����
                }
                else // ��Ŭ���� �������ٸ� Idle ���·�
                {
                    player.ChangeState(EPlayerState.Idle);
                }
            }
        }
    }

    public override void Exit()
    {
        player.Animator.speed = 1f; // ���¸� ���� �� �׻� �ִϸ��̼� �ӵ� ����
    }

    public bool IsWaitingForFire()
    {
        return waitingForFire;
    }
}

public class MeleeAttackState : BaseState
{
    private float attackAnimDuration; // �ִϸ��̼� ���̸� ������ ����
    private readonly float ATTACK_COOLDOWN = 1f; // ���� ���� ��Ÿ�� (��: 1��)

    // ���� ���� ���� (PlayerController���� ó���ϵ��� ���� ����)
    // private readonly float ATTACK_RANGE = 1.5f;
    // private Transform attackPoint; // PlayerController���� �Ҵ�ްų� ã�ƾ� ��
    // private LayerMask hittableLayers; // PlayerController���� ����

    private float stateTimer;       // ���°� ���۵� �� ��� �ð�
    private bool damageDealt;       // �̹� ���ݿ��� �������� �̹� �������� ���� (���� Ÿ�ݿ�)
    private float damageStartTime = 0.1f; // �ִϸ��̼� ���� �� ������ ���� ���� �ð�
    private float damageEndTime = 0.4f;   // ������ ���� ���� �ð�

    public MeleeAttackState(PlayerController player) : base(player)
    {
        // attackPoint = player.MeleeAttackPoint; // PlayerController�� �̷� ������ �ִٰ� ����
        // hittableLayers = player.EnemyLayers;   // PlayerController�� �̷� ������ �ִٰ� ����
    }

    public override void Enter()
    {
        stateTimer = 0f;
        damageDealt = false;
        player.Animator.Play("MeleeAttack");
        player.Animator.Update(0f); // �ִϸ��̼� ��� �ݿ�

        // �ִϸ��̼� ���� �������� (������, ��Ȯ�� ��� ����)
        AnimationClip[] clips = player.Animator.runtimeAnimatorController.animationClips;
        AnimationClip clip = System.Array.Find(clips, c => c.name == "MeleeAttack");
        if (clip != null)
        {
            attackAnimDuration = clip.length;
        }
        player.SetLastMeleeAttackTime(); // PlayerController�� ��Ÿ�� ��� �Լ� ȣ��
    }

    public override void Execute()
    {
        stateTimer += Time.deltaTime;

        // Ư�� Ÿ�ֿ̹� ���� ���� ����
        if (!damageDealt && stateTimer >= damageStartTime && stateTimer <= damageEndTime)
        {
            // PlayerController�� ���� ���� ���� ������ ���� ����
            // player.PerformMeleeDamageCheck(player.Status.ATK.Value, ATTACK_RANGE, attackPoint, hittableLayers);
            Debug.Log($"���� ���� ������ : {player.Status.ATK.Value}"); // ���� ������ ������ ���� ����
            damageDealt = true; // �� ���� ���� ���¿��� ���� �� �������� ���� �ʵ��� (�ִϸ��̼ǿ� ���� ����)
        }

        // �ִϸ��̼� ��� �Ϸ� (�Ǵ� ���� ���� �ð� �Ϸ�) �� Idle ���·� ��ȯ
        if (stateTimer >= attackAnimDuration)
        {
            player.ChangeState(EPlayerState.Idle);
        }
    }

    public override void Exit()
    {
        
    }
}