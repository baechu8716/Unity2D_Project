// BossController.cs
using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour, IDamageable
{
    [Header("Core References")]
    public Transform playerTransform;
    public Animator Animator { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public BossStatus Status { get; private set; } // BossStatus 사용
    public BossStateMachine StateMachine { get; private set; } // BossStateMachine 참조

    [Header("Stats & Combat Base")]
    [SerializeField] private float initialMaxHealth = 500f;
    [SerializeField] private int initialAttackPower = 20;
    public float moveSpeed = 3f;
    [SerializeField] private float pushForceOnMeleeHit = 5f;
    [SerializeField] public Transform meleeAttackPoint; // 근접 공격 판정 위치 (BossMeleeAttackState에서 사용 가능하도록)


    [Header("Detection & Movement")]
    public float playerDetectionRange = 15f;
    public float maintainDistanceRange = 3f;
    private bool isPlayerDead;

    [Header("General Attack (Ranged/Melee)")]
    public float generalAttackCooldown = 3f;
    public float CurrentGeneralAttackCooldown { get; set; } // 일반 공격 현재 쿨타임
    public float rangedAttackDistanceThreshold = 7f; // 원거리/근접 판단 기준 거리
    [SerializeField] private GameObject rangedAttackPrefab; // 원거리 공격 프리팹

    [Header("Flame Skill")]
    [SerializeField] private GameObject FlameSkillPrefab; // 화염 기둥 프리팹
    public float FlameSkillInterval = 20f; // 화염 기둥 스킬 간격
    public float CurrentFlameSkillCooldown { get; set; } // 화염 기둥 현재 쿨타임
    [SerializeField] private float FlameSpawnOffsetHorizontal = 2f; // 화염 기둥 수평 오프셋
    [SerializeField] private float FlameSpawnOffsetBetweenPillars = 1.5f; // 기둥 간 오프셋

    [Header("Flying Skill")]
    [SerializeField] private GameObject fireRainPrefab; // 불비 프리팹
    public float flyingSkillInterval = 60f; // 비행 스킬 간격
    public float CurrentFlyingSkillCooldown { get; set; } // 비행 스킬 현재 쿨타임
    [SerializeField] private float flyingSkillDuration = 10f; // 비행 지속 시간
    [SerializeField] private float flyHeight = 5f; // 비행 높이
    [SerializeField] private float fireRainInterval = 0.5f; // 불비 발사 간격
    [SerializeField] private Vector2 flyingHorizontalMoveRange = new Vector2(-5f, 5f); // 비행 중 수평 이동 범위
    private Coroutine flyingSkillCoroutine;
    private float currentFlyingSkillActiveTime;
    private float currentFireRainCooldown;
    private Vector3 initialGroundPosition;

    [Header("UI")] // UI 관련 헤더 추가
    public GameObject bossHpUiGameObject; // 인스펙터에서 보스 HP UI 게임 오브젝트를 연결


    private bool isFacingRight = true; // 초기 방향 (Awake에서 스케일 기반으로 재설정)

    void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        Rb = GetComponent<Rigidbody2D>();
        Status = new BossStatus(initialMaxHealth, initialAttackPower); 

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        StateMachine = new BossStateMachine(this);

        // FSM 상태 등록
        StateMachine.AddState(EBossState.Idle, new BossIdleState(this, StateMachine));
        StateMachine.AddState(EBossState.Chase, new BossChaseState(this, StateMachine));
        StateMachine.AddState(EBossState.ChooseAttack, new BossChooseAttackState(this, StateMachine));
        StateMachine.AddState(EBossState.RangedAttack, new BossRangedAttackState(this, StateMachine));
        StateMachine.AddState(EBossState.MeleeAttack, new BossMeleeAttackState(this, StateMachine));
        StateMachine.AddState(EBossState.FlameAttack, new BossFlameSkillState(this, StateMachine));
        StateMachine.AddState(EBossState.FlyingAttack, new BossFlyingSkillState(this, StateMachine));
        StateMachine.AddState(EBossState.Die, new BossDieState(this, StateMachine));
        StateMachine.AddState(EBossState.Hit, new BossHitState(this, StateMachine));

        CurrentGeneralAttackCooldown = 0f; // 초기 쿨타임 설정
        CurrentFlameSkillCooldown = FlameSkillInterval; 
        CurrentFlyingSkillCooldown = flyingSkillInterval; 

        // isFacingRight 초기화: 스프라이트가 기본적으로 오른쪽을 보고 있다면 true, 왼쪽이면 false. 스케일로 판단.
        if (transform.localScale.x < 0) isFacingRight = true;
        else isFacingRight = false;


        Status.HP.OnValueChanged += HandleHealthChanged; // HP 변경 시 이벤트 핸들러 등록
    }

    void OnDestroy()
    {
        if (Status != null && Status.HP != null)
            Status.HP.OnValueChanged -= HandleHealthChanged; // 이벤트 핸들러 해제
    }

    private void HandleHealthChanged(float newHP)
    {
        Debug.Log($"보스 HP : {newHP}");
        if (newHP <= 0 && StateMachine.CurrentState is not BossDieState) // Die 상태 중복 진입 방지 (DieState 추가 시)
        {
            Die();
        }
    }

    void Start()
    {
        initialGroundPosition = transform.position; // 최초 지상 위치 저장
        StateMachine.ChangeState(EBossState.Idle); // 초기 상태를 Idle로 설정
    }

    void Update()
    {
        // 1. 보스 자신의 사망 상태 먼저 처리
        if (Status != null && Status.HP.Value <= 0)
        {
            if (StateMachine.CurrentState is not BossDieState && enabled)
            {
                Die(); // BossDieState로 전환
            }
            // 보스가 죽으면 UI를 확실히 끈다 (BossDieState.Enter 또는 HandleDeathEffectsAndCleanup에서도 처리)
            if (bossHpUiGameObject != null && bossHpUiGameObject.activeSelf)
            {
                bossHpUiGameObject.SetActive(false);
            }
            // 스크립트가 비활성화되었거나 Die 상태이면 더 이상 업데이트하지 않음
            if (!enabled || StateMachine.CurrentState is BossDieState)
            {
                // StateMachine.Update(); // Die 상태의 Execute 로직이 있다면 실행되도록 할 수 있음
                return;
            }
        }

        // 2. 플레이어 유효성 검사 및 UI 활성화/비활성화 로직
        bool playerIsValidAndAlive = (playerTransform != null && playerTransform.gameObject.activeInHierarchy);

        if (playerIsValidAndAlive)
        {
            // 플레이어가 유효하고 살아있다면, 거리 기반으로 UI 관리
            if (bossHpUiGameObject != null)
            {
                float distanceToPlayer = GetPlayerDistance();
                if (distanceToPlayer <= playerDetectionRange)
                {
                    if (!bossHpUiGameObject.activeSelf) // 현재 비활성화 상태일 때만 활성화
                    {
                        bossHpUiGameObject.SetActive(true);
                    }
                }
                else
                {
                    if (bossHpUiGameObject.activeSelf) // 현재 활성화 상태일 때만 비활성화
                    {
                        bossHpUiGameObject.SetActive(false);
                    }
                }
            }

            // 플레이어가 유효할 때 정상적인 보스 로직 수행
            HandleCooldowns();
            StateMachine.Update();
            // Die 상태가 아닐 때만 플레이어 방향으로 Flip (FlipTowardsPlayer는 playerTransform이 null이 아닐 때만 호출되어야 함)
            if (StateMachine.CurrentState is not BossDieState)
            {
                FlipTowardsPlayer();
            }
        }
        else // 플레이어가 유효하지 않거나 죽었을 경우
        {
            // HP UI 비활성화
            if (bossHpUiGameObject != null && bossHpUiGameObject.activeSelf)
            {
                bossHpUiGameObject.SetActive(false);
            }

            // 플레이어가 유효하지 않다면 보스는 Idle 상태로 (이미 죽은 상태가 아니라면)
            if (StateMachine.CurrentState is not BossIdleState && StateMachine.CurrentState is not BossDieState)
            {
                StateMachine.ChangeState(EBossState.Idle);
            }
            // 현재 상태(대부분 Idle)의 Execute는 계속 실행되도록 함
            StateMachine.Update();
        }
    }

    public void NotifyPlayerDeath()
    {
        isPlayerDead = true; // isPlayerDead 플래그가 있다면 계속 사용
        Debug.Log("보스: 플레이어 사망 알림 받음. Idle 상태로 전환 준비.");
        if (StateMachine.CurrentState is not BossIdleState && StateMachine.CurrentState is not BossDieState)
        {
            StateMachine.ChangeState(EBossState.Idle);
        }
        // 플레이어가 죽었으므로 보스 HP UI도 비활성화
        if (bossHpUiGameObject != null && bossHpUiGameObject.activeSelf)
        {
            bossHpUiGameObject.SetActive(false);
        }
    }

    private void HandleCooldowns()
    {
        if (CurrentGeneralAttackCooldown > 0) CurrentGeneralAttackCooldown -= Time.deltaTime; 
        if (CurrentFlameSkillCooldown > 0) CurrentFlameSkillCooldown -= Time.deltaTime; 
        if (CurrentFlyingSkillCooldown > 0) CurrentFlyingSkillCooldown -= Time.deltaTime; 
    }

    public void FlipTowardsPlayer()
    {
        if (playerTransform == null) return;
        float directionToPlayerX = playerTransform.position.x - transform.position.x;
        if (directionToPlayerX > 0.1f && !isFacingRight) Flip(); 
        else if (directionToPlayerX < -0.1f && isFacingRight) Flip(); 
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

    public void PerformRangedAttackAction()
    {
        if (rangedAttackPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, 0f);

            GameObject projectileInstance = Instantiate(rangedAttackPrefab, spawnPosition, Quaternion.identity);

            Vector2 launchDirection = isFacingRight ? Vector2.right : Vector2.left; 

            EffectDamageHandler damageHandler = projectileInstance.GetComponent<EffectDamageHandler>();
            if (damageHandler != null)
            {
                damageHandler.Initialize(Status.ATK, gameObject, "Player"); 
            }

            Rigidbody2D rb = projectileInstance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float projectileSpeed = 10f; // 투사체 속도
                rb.velocity = launchDirection * projectileSpeed; // X축으로만 속도 설정

                float angle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg; 
                projectileInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle); 
            }
        }
        CurrentGeneralAttackCooldown = generalAttackCooldown; 
    }

    public void PerformMeleeAttackAction()
    {
        if (playerTransform != null && GetPlayerDistance() <= maintainDistanceRange + 0.5f) 
        {
            Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
            PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>(); 
            if (playerRb != null)
            {
                // 넉백 방향 (X축만)
                float direction = Mathf.Sign(playerTransform.position.x - transform.position.x); 
                Vector2 knockback = new Vector2(direction * pushForceOnMeleeHit, 0f); 
                playerRb.velocity = new Vector2(knockback.x, 0f); 

            }
        }

        CurrentGeneralAttackCooldown = generalAttackCooldown; 
    }


    public void PerformFlameSkillAction()
    {
        if (FlameSkillPrefab != null)
        {
            Vector3 baseOffsetDir = isFacingRight ? Vector3.right : Vector3.left; // 보스 방향 기준

            // 오른쪽 그룹
            Instantiate(FlameSkillPrefab, transform.position + baseOffsetDir * FlameSpawnOffsetHorizontal, Quaternion.identity); 
            Instantiate(FlameSkillPrefab, transform.position + baseOffsetDir * (FlameSpawnOffsetHorizontal + FlameSpawnOffsetBetweenPillars), Quaternion.identity); 
            // 왼쪽 그룹
            Instantiate(FlameSkillPrefab, transform.position - baseOffsetDir * FlameSpawnOffsetHorizontal, Quaternion.identity); 
            Instantiate(FlameSkillPrefab, transform.position - baseOffsetDir * (FlameSpawnOffsetHorizontal + FlameSpawnOffsetBetweenPillars), Quaternion.identity); 

        }
        CurrentFlameSkillCooldown = FlameSkillInterval; 
    }

    public void StartFlyingSequence()
    {
        if (flyingSkillCoroutine != null) StopCoroutine(flyingSkillCoroutine); 
        flyingSkillCoroutine = StartCoroutine(FlyingSkillRoutine()); 
        CurrentFlyingSkillCooldown = flyingSkillInterval; 
    }

    private IEnumerator FlyingSkillRoutine()
    {
        Vector3 currentFlyTargetPos = new Vector3(transform.position.x, initialGroundPosition.y + flyHeight, transform.position.z); 

        float flyUpDuration = 1f;
        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        while (elapsedTime < flyUpDuration)
        {
            transform.position = Vector3.Lerp(startPos, currentFlyTargetPos, elapsedTime / flyUpDuration); 
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = currentFlyTargetPos; 

        currentFlyingSkillActiveTime = 0f;
        currentFireRainCooldown = 0f;

        while (currentFlyingSkillActiveTime < flyingSkillDuration) 
        {
            float targetX = initialGroundPosition.x + Random.Range(flyingHorizontalMoveRange.x, flyingHorizontalMoveRange.y); 
            Vector3 moveTargetInAir = new Vector3(targetX, transform.position.y, transform.position.z);

            float moveDurationThisSegment = Random.Range(1.0f, 2.0f);
            elapsedTime = 0f;
            Vector3 segmentStartPos = transform.position;
            while (elapsedTime < moveDurationThisSegment)
            {
                transform.position = Vector3.Lerp(segmentStartPos, moveTargetInAir, elapsedTime / moveDurationThisSegment); 
                elapsedTime += Time.deltaTime;

                currentFireRainCooldown -= Time.deltaTime;
                if (currentFireRainCooldown <= 0) 
                {
                    if (fireRainPrefab != null) 
                    {
                        Vector3 rainSpawnPos = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z); // 불비 생성 위치
                        GameObject fireBallInstance = Instantiate(fireRainPrefab, rainSpawnPos, Quaternion.identity); 
                        Projectile fireRainProjectile = fireBallInstance.GetComponent<Projectile>();
                        if (fireRainProjectile != null)
                        {
                            fireRainProjectile.Initialize(Vector2.down, Status.ATK, gameObject); // 불비 데미지 및 방향 설정
                        }
                    }
                    currentFireRainCooldown = fireRainInterval; 
                }
                currentFlyingSkillActiveTime += Time.deltaTime; 
                if (currentFlyingSkillActiveTime >= flyingSkillDuration) break; 
                yield return null;
            }
            if (currentFlyingSkillActiveTime >= flyingSkillDuration) break; 
        }

        elapsedTime = 0f;
        float landDuration = 1f;
        Vector3 beforeLandPosition = transform.position;
        // 착지 목표: 현재 x, z와 초기 y
        Vector3 finalLandingPosition = new Vector3(transform.position.x, initialGroundPosition.y, transform.position.z); 

        while (elapsedTime < landDuration)
        {
            transform.position = Vector3.Lerp(beforeLandPosition, finalLandingPosition, elapsedTime / landDuration); 
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = finalLandingPosition; 
        flyingSkillCoroutine = null;
        StateMachine.ChangeState(EBossState.Idle); // 비행 후 Idle 상태로
    }

    public void StopFlyingSequenceAndLand()
    {
        if (flyingSkillCoroutine != null)
        {
            StopCoroutine(flyingSkillCoroutine); 
            StartCoroutine(ForceLandRoutine(initialGroundPosition.y)); 
        }
        flyingSkillCoroutine = null;
    }

    private IEnumerator ForceLandRoutine(float targetY)
    {
        Vector3 currentPos = transform.position;
        Vector3 targetFloorPos = new Vector3(currentPos.x, targetY, currentPos.z); 

        float elapsedTime = 0f;
        float landDuration = 0.5f; // 강제 착지 시간
        while (elapsedTime < landDuration)
        {
            transform.position = Vector3.Lerp(currentPos, targetFloorPos, elapsedTime / landDuration); 
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetFloorPos; 
        StateMachine.ChangeState(EBossState.Idle); // 강제 착지 후 Idle 상태로
    }

    public bool IsFlying()
    {
        return flyingSkillCoroutine != null; // 코루틴 실행 여부로 비행 상태 판단
    }

    public void TakeDamage(float damageAmount)
    {
        // 이미 죽었거나, 현재 Hit 또는 Die 상태이거나, 비행 중(무적 판정이라면) 등 특정 상태에서 데미지 무시
        if (Status.HP.Value <= 0 ||
            StateMachine.CurrentState is BossDieState ||
            StateMachine.CurrentState is BossHitState ||
            (StateMachine.CurrentState is BossFlyingSkillState && IsFlying())) // 비행 중 무적
        {
            return;
        }

        Status.HP.Value -= damageAmount;
        Debug.Log($"보스 {damageAmount} 데미지 받음. 현재 HP: {Status.HP.Value}");

        if (Status.HP.Value > 0)
        {
            // 피격 시 어떤 상태에 있든 Hit 상태로 전환
            // 이렇게 하면 이전 상태의 로직이 중단되고 Hit 상태의 로직(애니메이션 재생 후 Idle/Chase)이 실행
            StateMachine.ChangeState(EBossState.Hit);
        }
    }


    private void Die()
    {
        if (StateMachine.CurrentState is BossDieState) return; // 이미 Die 상태면 중복 실행 방지

        Debug.Log("보스 사망");
        StateMachine.ChangeState(EBossState.Die); // FSM을 통해 Die 상태로 전환

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameClearUI();
        }
    }

    public void HandleDeathEffectsAndCleanup()
    {
        if (bossHpUiGameObject != null) // 보스 사망 시 HP UI 비활성화
        {
            bossHpUiGameObject.SetActive(false);
        }
        StopAllCoroutines();
        if (Rb != null)
        {
            Rb.velocity = Vector2.zero;
            Rb.isKinematic = true;
        }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 5f); // 5초 뒤 오브젝트 파괴
        enabled = false; // 스크립트 비활성화
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangedAttackDistanceThreshold); // 원거리 공격 임계 거리 시각화

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maintainDistanceRange); // 유지 거리 시각화

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange); // 플레이어 감지 범위 시각화
    }
}