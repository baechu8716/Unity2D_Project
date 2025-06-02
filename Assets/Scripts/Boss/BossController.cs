using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Animator Animator { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public Transform firePoint; // 원거리 공격 발사 위치
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
    public float maintainDistanceRange = 3f; // 이 거리 안쪽으로 들어오면 멈추거나 뒤로 물러날 수 있음

    [Header("Attack Settings")]
    public float generalAttackCooldown = 3f;
    public float rangedAttackDistanceThreshold = 7f; // 이 거리보다 멀면 원거리
    // 근접 공격 범위, 판정 등은 MeleeAttackState 또는 여기서 직접 처리

    [Header("Flame Skill")]
    public float FlameSkillInterval = 20f;
    public float FlameSpawnOffset = 3f; // 보스 기준 양쪽으로 기둥 생성 시 간격

    [Header("Flying Skill")]
    public float flyingSkillInterval = 60f;
    public float flyingSkillDuration = 10f;
    public float flyHeight = 5f; // 현재 위치 기준 비행 높이
    public float fireRainInterval = 0.5f; // 불비 내리는 간격
    public Vector2 flyingHorizontalMoveRange = new Vector2(-5f, 5f); // 비행 중 좌우 이동 범위 (월드 좌표 기준 또는 보스 초기 위치 기준)

    // 내부 타이머
    public float CurrentGeneralAttackCooldown { get; set; }
    public float CurrentFlameSkillCooldown { get; set; }
    public float CurrentFlyingSkillCooldown { get; set; }
    private float currentFlyingSkillActiveTime; // 비행 스킬 지속 시간 측정용
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

        CurrentGeneralAttackCooldown = 0f; // 시작 시 바로 공격 가능하도록 (또는 약간의 딜레이)
        CurrentFlameSkillCooldown = FlameSkillInterval; // 시작 후 일정 시간 뒤 발동
        CurrentFlyingSkillCooldown = flyingSkillInterval; // 시작 후 일정 시간 뒤 발동
    }

    void Start()
    {
        StateMachine.ChangeState(EBossState.Idle);
    }

    void Update()
    {
        if (playerTransform == null) return; // 플레이어가 없으면 동작 안함

        HandleCooldowns();
        StateMachine.Update(); // 현재 상태의 Execute() 호출
        FlipTowardsPlayer(); // 플레이어 방향 보도록
    }

    private void HandleCooldowns()
    {
        if (CurrentGeneralAttackCooldown > 0) CurrentGeneralAttackCooldown -= Time.deltaTime;
        if (CurrentFlameSkillCooldown > 0) CurrentFlameSkillCooldown -= Time.deltaTime;
        if (CurrentFlyingSkillCooldown > 0) CurrentFlyingSkillCooldown -= Time.deltaTime;

        // 비행 스킬 지속 시간 및 불비 쿨다운은 FlyingSkillState에서 직접 관리하거나 여기서 할 수도 있음
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
            projectile.transform.right = direction; // Sprite 회전 (transform.rotation으로도 가능)

            // Rigidbody2D를 통한 이동 처리
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float speed = 8f; // 발사 속도
                rb.velocity = direction * speed;
            }

            // 일정 시간 후 제거
            Destroy(projectile, 3f);
        }

        CurrentGeneralAttackCooldown = generalAttackCooldown;
    }

    public void PerformMeleeAttack()
    {
        //Animator.Play("MeleeAttack");
        // 여기에 근접 공격 판정 로직 (예: 특정 시점에 OverlapCircle 등 사용)
        // 예시: StartCoroutine(MeleeAttackDamagePhase(0.5f, 1f)); // 0.5초 뒤 1초간 공격 판정
        CurrentGeneralAttackCooldown = generalAttackCooldown;
    }

    public void PerformFlameSkill()
    {
        //Animator.Play("Idle"); // "Idle" 애니메이션 대신 "FlameSkill_Cast"와 같은 전용 애니메이션 권장
        // 양쪽에 기둥 생성
        if (FlameSkillPrefab != null)
        {
            Instantiate(FlameSkillPrefab, transform.position + Vector3.left * FlameSpawnOffset, Quaternion.identity);
            Instantiate(FlameSkillPrefab, transform.position + Vector3.right * FlameSpawnOffset, Quaternion.identity);
        }
        CurrentFlameSkillCooldown = FlameSkillInterval;
    }


    // Flying Skill 관련 코루틴 및 상태 관리
    private Coroutine flyingSkillCoroutine;
    public void StartFlyingSequence()
    {
        if (flyingSkillCoroutine != null) StopCoroutine(flyingSkillCoroutine);
        flyingSkillCoroutine = StartCoroutine(FlyingSkillRoutine());
        CurrentFlyingSkillCooldown = flyingSkillInterval;
    }

    private IEnumerator FlyingSkillRoutine()
    {
        Animator.Play("Fly"); // "Fly" 애니메이션
        // 상태 변경 로직은 FlyingSkillState에서 처리할 것이므로 여기서는 애니메이션과 실제 동작만

        Vector3 originalPosition = transform.position;
        Vector3 flyTargetPosition = new Vector3(transform.position.x, originalPosition.y + flyHeight, transform.position.z); // 또는 고정된 Y 값

        // 위로 날아오르기
        float flyUpDuration = 1f; // 예시: 1초간 날아오름
        float elapsedTime = 0f;
        while (elapsedTime < flyUpDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, flyTargetPosition, elapsedTime / flyUpDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = flyTargetPosition;

        // 비행 중 공격
        currentFlyingSkillActiveTime = 0f;
        currentFireRainCooldown = 0f;
        float startX = transform.position.x; // 초기 x 위치 기억

        while (currentFlyingSkillActiveTime < flyingSkillDuration)
        {
            // 좌우로 랜덤 이동
            // 목표 X 위치를 flyingHorizontalMoveRange 내에서 설정 (현재 위치 기준 또는 월드 기준)
            float targetX = startX + Random.Range(flyingHorizontalMoveRange.x, flyingHorizontalMoveRange.y);
            Vector3 moveTargetPos = new Vector3(targetX, transform.position.y, transform.position.z);

            // 이동 (간단한 Lerp 또는 다른 이동 방식)
            float moveDuration = 1.5f; // 예시: 새 위치까지 1.5초
            elapsedTime = 0f;
            Vector3 currentFlyPos = transform.position;
            while (elapsedTime < moveDuration)
            {
                transform.position = Vector3.Lerp(currentFlyPos, moveTargetPos, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                // 불비 투하 로직 (이동 중에도 계속)
                if (currentFireRainCooldown <= 0)
                {
                    if (fireRainPrefab != null)
                    {
                        Instantiate(fireRainPrefab, transform.position, Quaternion.identity); // firePoint가 있다면 그 위치에서
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

        // 착지
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
        Animator.Play("Idle"); // 또는 착지 애니메이션 후 Idle

        Debug.Log("Boss flying skill finished.");
        // FlyingSkillState에서 Idle/Chase로 전환
        StateMachine.ChangeState(EBossState.Idle); // 예시, 실제로는 상태 클래스에서 처리
    }

    public void StopFlyingSequence() // 외부에서 비행을 중단시킬 경우 (예: 특정 조건, 페이즈 변경 등)
    {
        if (flyingSkillCoroutine != null)
        {
            StopCoroutine(flyingSkillCoroutine);
            // 필요하다면 즉시 착지 로직 또는 원래 위치 복귀 로직
            transform.position = new Vector3(transform.position.x, transform.position.y - flyHeight, transform.position.z); // 간단 복귀
            Animator.Play("Idle");
        }
    }

    public bool IsFlyingSkillActive()
    {
        return currentFlyingSkillActiveTime < flyingSkillDuration && currentFlyingSkillActiveTime > 0; // 혹은 StateMachine.CurrentState is BossFlyingSkillState
    }

    // 플레이어에게 데미지를 입히는 함수 (IDamageable 구현 시 사용)
    // public void TakeDamage(float damage) { ... }
}
