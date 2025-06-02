using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Animator Animator { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public Transform firePoint; // ���Ÿ� ���� �߻� ��ġ
    public GameObject rangedAttackPrefab;
    public GameObject FlameSkillPrefab;
    public GameObject fireRainPrefab;

    [Header("Stats")]
    public float maxHealth = 500f;
    public float currentHealth;
    public int attackPower = 20;
    public float moveSpeed = 3f;

    [Header("Detection & Movement")]
    public float playerDetectionRange = 15f;
    public float maintainDistanceRange = 3f; // �� �Ÿ� �������� ������ ���߰ų� �ڷ� ������ �� ����

    [Header("Attack Settings")]
    public float generalAttackCooldown = 3f;
    public float rangedAttackDistanceThreshold = 7f; // �� �Ÿ����� �ָ� ���Ÿ�
    // ���� ���� ����, ���� ���� MeleeAttackState �Ǵ� ���⼭ ���� ó��

    [Header("Flame Skill")]
    public float FlameSkillInterval = 20f;
    public float FlameSpawnOffset = 3f; // ���� ���� �������� ��� ���� �� ����

    [Header("Flying Skill")]
    public float flyingSkillInterval = 60f;
    public float flyingSkillDuration = 10f;
    public float flyHeight = 5f; // ���� ��ġ ���� ���� ����
    public float fireRainInterval = 0.5f; // �Һ� ������ ����
    public Vector2 flyingHorizontalMoveRange = new Vector2(-5f, 5f); // ���� �� �¿� �̵� ���� (���� ��ǥ ���� �Ǵ� ���� �ʱ� ��ġ ����)

    // ���� Ÿ�̸�
    public float CurrentGeneralAttackCooldown { get; set; }
    public float CurrentFlameSkillCooldown { get; set; }
    public float CurrentFlyingSkillCooldown { get; set; }
    private float currentFlyingSkillActiveTime; // ���� ��ų ���� �ð� ������
    private float currentFireRainCooldown;

    public BossStateMachine StateMachine { get; private set; }
    private bool isFacingRight = true;

    void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        Rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            playerTransform = playerObj.transform;
        }

        StateMachine = new BossStateMachine(this);
        StateMachine.AddState(EBossState.Idle, new BossIdleState(this, StateMachine));
        StateMachine.AddState(EBossState.Chase, new BossChaseState(this, StateMachine));
        StateMachine.AddState(EBossState.ChooseAttack, new BossChooseAttackState(this, StateMachine));
        StateMachine.AddState(EBossState.RangedAttack, new BossRangedAttackState(this, StateMachine));
        StateMachine.AddState(EBossState.MeleeAttack, new BossMeleeAttackState(this, StateMachine));
        StateMachine.AddState(EBossState.FlameAttack, new BossFlameSkillState(this, StateMachine));
        StateMachine.AddState(EBossState.FlyingAttack, new BossFlyingSkillState(this, StateMachine));

        CurrentGeneralAttackCooldown = 0f; // ���� �� �ٷ� ���� �����ϵ��� (�Ǵ� �ణ�� ������)
        CurrentFlameSkillCooldown = FlameSkillInterval; // ���� �� ���� �ð� �� �ߵ�
        CurrentFlyingSkillCooldown = flyingSkillInterval; // ���� �� ���� �ð� �� �ߵ�
    }

    void Start()
    {
        StateMachine.ChangeState(EBossState.Idle);
    }

    void Update()
    {
        if (playerTransform == null) return; // �÷��̾ ������ ���� ����

        HandleCooldowns();
        StateMachine.Update(); // ���� ������ Execute() ȣ��
        FlipTowardsPlayer(); // �÷��̾� ���� ������
    }

    private void HandleCooldowns()
    {
        if (CurrentGeneralAttackCooldown > 0) CurrentGeneralAttackCooldown -= Time.deltaTime;
        if (CurrentFlameSkillCooldown > 0) CurrentFlameSkillCooldown -= Time.deltaTime;
        if (CurrentFlyingSkillCooldown > 0) CurrentFlyingSkillCooldown -= Time.deltaTime;

        // ���� ��ų ���� �ð� �� �Һ� ��ٿ��� FlyingSkillState���� ���� �����ϰų� ���⼭ �� ���� ����
    }

    public void FlipTowardsPlayer()
    {
        if (playerTransform == null) return;
        float directionToPlayer = playerTransform.position.x - transform.position.x;
        if (directionToPlayer > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (directionToPlayer < 0 && isFacingRight)
        {
            Flip();
        }
    }

    public void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    public float GetPlayerDistance()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector2.Distance(transform.position, playerTransform.position);
    }

    public Vector2 GetDirectionToPlayer()
    {
        if (playerTransform == null) return Vector2.zero;
        return (playerTransform.position - transform.position).normalized;
    }

    public void PerformRangedAttack()
    {
        if (rangedAttackPrefab != null && firePoint != null)
        {
            GameObject projectile = Instantiate(rangedAttackPrefab, firePoint.position, Quaternion.identity);

            Vector2 direction = GetDirectionToPlayer().normalized;
            projectile.transform.right = direction; // Sprite ȸ�� (transform.rotation���ε� ����)

            // Rigidbody2D�� ���� �̵� ó��
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float speed = 8f; // �߻� �ӵ�
                rb.velocity = direction * speed;
            }

            // ���� �ð� �� ����
            Destroy(projectile, 3f);
        }

        CurrentGeneralAttackCooldown = generalAttackCooldown;
    }

    public void PerformMeleeAttack()
    {
        //Animator.Play("MeleeAttack");
        // ���⿡ ���� ���� ���� ���� (��: Ư�� ������ OverlapCircle �� ���)
        // ����: StartCoroutine(MeleeAttackDamagePhase(0.5f, 1f)); // 0.5�� �� 1�ʰ� ���� ����
        CurrentGeneralAttackCooldown = generalAttackCooldown;
    }

    public void PerformFlameSkill()
    {
        //Animator.Play("Idle"); // "Idle" �ִϸ��̼� ��� "FlameSkill_Cast"�� ���� ���� �ִϸ��̼� ����
        // ���ʿ� ��� ����
        if (FlameSkillPrefab != null)
        {
            Instantiate(FlameSkillPrefab, transform.position + Vector3.left * FlameSpawnOffset, Quaternion.identity);
            Instantiate(FlameSkillPrefab, transform.position + Vector3.right * FlameSpawnOffset, Quaternion.identity);
        }
        CurrentFlameSkillCooldown = FlameSkillInterval;
    }


    // Flying Skill ���� �ڷ�ƾ �� ���� ����
    private Coroutine flyingSkillCoroutine;
    public void StartFlyingSequence()
    {
        if (flyingSkillCoroutine != null) StopCoroutine(flyingSkillCoroutine);
        flyingSkillCoroutine = StartCoroutine(FlyingSkillRoutine());
        CurrentFlyingSkillCooldown = flyingSkillInterval;
    }

    private IEnumerator FlyingSkillRoutine()
    {
        Animator.Play("Fly"); // "Fly" �ִϸ��̼�
        // ���� ���� ������ FlyingSkillState���� ó���� ���̹Ƿ� ���⼭�� �ִϸ��̼ǰ� ���� ���۸�

        Vector3 originalPosition = transform.position;
        Vector3 flyTargetPosition = new Vector3(transform.position.x, originalPosition.y + flyHeight, transform.position.z); // �Ǵ� ������ Y ��

        // ���� ���ƿ�����
        float flyUpDuration = 1f; // ����: 1�ʰ� ���ƿ���
        float elapsedTime = 0f;
        while (elapsedTime < flyUpDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, flyTargetPosition, elapsedTime / flyUpDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = flyTargetPosition;

        // ���� �� ����
        currentFlyingSkillActiveTime = 0f;
        currentFireRainCooldown = 0f;
        float startX = transform.position.x; // �ʱ� x ��ġ ���

        while (currentFlyingSkillActiveTime < flyingSkillDuration)
        {
            // �¿�� ���� �̵�
            // ��ǥ X ��ġ�� flyingHorizontalMoveRange ������ ���� (���� ��ġ ���� �Ǵ� ���� ����)
            float targetX = startX + Random.Range(flyingHorizontalMoveRange.x, flyingHorizontalMoveRange.y);
            Vector3 moveTargetPos = new Vector3(targetX, transform.position.y, transform.position.z);

            // �̵� (������ Lerp �Ǵ� �ٸ� �̵� ���)
            float moveDuration = 1.5f; // ����: �� ��ġ���� 1.5��
            elapsedTime = 0f;
            Vector3 currentFlyPos = transform.position;
            while (elapsedTime < moveDuration)
            {
                transform.position = Vector3.Lerp(currentFlyPos, moveTargetPos, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                // �Һ� ���� ���� (�̵� �߿��� ���)
                if (currentFireRainCooldown <= 0)
                {
                    if (fireRainPrefab != null)
                    {
                        Instantiate(fireRainPrefab, transform.position, Quaternion.identity); // firePoint�� �ִٸ� �� ��ġ����
                    }
                    currentFireRainCooldown = fireRainInterval;
                }
                else
                {
                    currentFireRainCooldown -= Time.deltaTime;
                }
                currentFlyingSkillActiveTime += Time.deltaTime;
                if (currentFlyingSkillActiveTime >= flyingSkillDuration) break;
                yield return null;
            }
            if (currentFlyingSkillActiveTime >= flyingSkillDuration) break;
        }

        // ����
        Debug.Log("Boss flying skill ending. Landing.");
        elapsedTime = 0f;
        float landDuration = 1f;
        Vector3 beforeLandPos = transform.position;
        while (elapsedTime < landDuration)
        {
            transform.position = Vector3.Lerp(beforeLandPos, originalPosition, elapsedTime / landDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
        Animator.Play("Idle"); // �Ǵ� ���� �ִϸ��̼� �� Idle

        Debug.Log("Boss flying skill finished.");
        // FlyingSkillState���� Idle/Chase�� ��ȯ
        StateMachine.ChangeState(EBossState.Idle); // ����, �����δ� ���� Ŭ�������� ó��
    }

    public void StopFlyingSequence() // �ܺο��� ������ �ߴܽ�ų ��� (��: Ư�� ����, ������ ���� ��)
    {
        if (flyingSkillCoroutine != null)
        {
            StopCoroutine(flyingSkillCoroutine);
            // �ʿ��ϴٸ� ��� ���� ���� �Ǵ� ���� ��ġ ���� ����
            transform.position = new Vector3(transform.position.x, transform.position.y - flyHeight, transform.position.z); // ���� ����
            Animator.Play("Idle");
        }
    }

    public bool IsFlyingSkillActive()
    {
        return currentFlyingSkillActiveTime < flyingSkillDuration && currentFlyingSkillActiveTime > 0; // Ȥ�� StateMachine.CurrentState is BossFlyingSkillState
    }

    // �÷��̾�� �������� ������ �Լ� (IDamageable ���� �� ���)
    // public void TakeDamage(float damage) { ... }
}
