using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DesignPattern;

public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("FSM & Core Components")]
    private StateMachine stateMachine;
    public PlayerMovement Movement { get; private set; }
    public PlayerStatus Status { get; private set; }     
    public Animator Animator { get; private set; }      

    [Header("Combat Stats & Settings")]
    [SerializeField] private int initialAttackPower = 10;
    [SerializeField] private float initialHealth = 100f;
    // Melee Attack
    [SerializeField] public float meleeAttackRange = 1.5f; 
    [SerializeField] public Transform meleeAttackPoint;   
    [SerializeField] public LayerMask enemyLayers;         
    [SerializeField] private float meleeAttackCooldown = 1f;
    private float lastMeleeAttackTime;

    [Header("Ranged Attack (Bow)")] 
    [SerializeField] private GameObject arrowPrefab;     // 화살 프리팹
    [SerializeField] private Transform firePoint;         // 발사 위치
    [SerializeField] private float maxFireAngle = 45f;    // 발사 가능 최대 각도
    [SerializeField] private float arrowDamageMultiplier = 1.0f; // 화살 데미지 배율 (인스펙터에서 설정 가능)

    [Header("Roll Settings")]
    [SerializeField] private float rollCooldown = 2f;
    private float lastRollTime;

    [Header("Aiming & Camera")]
    [SerializeField] public GameObject aimUIPrefab;     // 조준 UI 프리팹
    [SerializeField] public CinemachineVirtualCamera virtualCamera; // 시네머신 가상 카메라
    [SerializeField] public float zoomInSize = 3f;     // 줌 인 크기
    [SerializeField] public float zoomOutSize = 6f;    // 줌 아웃 크기
    [SerializeField] public LineRenderer angleIndicatorRenderer; // 각도 지시선 LineRenderer
    [SerializeField] public float angleIndicatorLength = 1.5f; // 각도 지시선 길이
    [SerializeField] public Color validAngleColor = Color.green; // 유효 각도 색상
    [SerializeField] public Color invalidAngleColor = Color.red; // 무효 각도 색상
    [SerializeField] public float zoomTransitionDuration = 0.3f; // 줌 전환에 걸리는 시간 (초)
    private Coroutine zoomCoroutine; // 현재 실행 중인 줌 코루틴을 저장하기 위한 변수
    private GameObject aimUIInstance;


    public bool HasPlayedJumpAnimation { get; set; } // 점프 애니메이션 재생 여부

    public float ATTACK_RANGE => meleeAttackRange; // ATTACK_RANGE 프로퍼티
    public Transform MeleeAttackPoint => meleeAttackPoint; // MeleeAttackPoint 프로퍼티
    public LayerMask EnemyLayers => enemyLayers; // EnemyLayers 프로퍼티


    void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Animator = GetComponentInChildren<Animator>();
        Status = new PlayerStatus(initialHealth, initialAttackPower); // Status 초기화

        stateMachine = new StateMachine(); 
        stateMachine.AddState(EPlayerState.Idle, new IdleState(this)); 
        stateMachine.AddState(EPlayerState.Move, new MoveState(this)); 
        stateMachine.AddState(EPlayerState.Jump, new JumpState(this)); 
        stateMachine.AddState(EPlayerState.Fall, new FallState(this)); 
        stateMachine.AddState(EPlayerState.Roll, new RollState(this)); 
        stateMachine.AddState(EPlayerState.Attack, new AttackState(this));     
        stateMachine.AddState(EPlayerState.MeleeAttack, new MeleeAttackState(this)); 
        stateMachine.AddState(EPlayerState.Hit, new HitState(this)); 
        stateMachine.AddState(EPlayerState.Die, new PlayerDieState(this)); 

        stateMachine.ChangeState(EPlayerState.Idle); // 초기 상태를 Idle로 설정

        if (aimUIPrefab != null)
            aimUIInstance = Instantiate(aimUIPrefab, Vector3.zero, Quaternion.identity); // 조준 UI 생성
        else
            Debug.LogError("AimUI Prefab is not assigned!");

        if (virtualCamera != null)
            virtualCamera.m_Lens.OrthographicSize = zoomOutSize; // 카메라 초기 줌 아웃

        lastRollTime = -rollCooldown; // 구르기 쿨타임 초기화
        lastMeleeAttackTime = -meleeAttackCooldown; // 근접 공격 쿨타임 초기화

        Status.HP.OnValueChanged += HandleHealthChanged; // HP 변경 시 이벤트 핸들러 등록
    }

    void OnDestroy()
    {
        if (Status != null && Status.HP != null)
            Status.HP.OnValueChanged -= HandleHealthChanged; // 이벤트 핸들러 해제
    }

    private void HandleHealthChanged(float newHP)
    {
        Debug.Log($"Player HP Changed: {newHP}");
        if (newHP <= 0)
        {
            Die();
        }
    }

    void Update()
    {
        stateMachine.Update(); // FSM 업데이트
        if (aimUIInstance != null) HandleAimUI(); // 조준 UI 처리
        HandleZoom(); // 카메라 줌 처리
    }

    void LateUpdate()
    {
        // LineRenderer 업데이트는 모든 위치 계산이 끝난 LateUpdate가 적합
        if (angleIndicatorRenderer != null && firePoint != null && aimUIInstance != null) HandleAngleIndicator(); // 각도 지시선 처리
    }

    public void ChangeState(EPlayerState newState)
    {
        stateMachine.ChangeState(newState); // 상태 변경
    }

    public bool CanRoll()
    {
        return Time.time >= lastRollTime + rollCooldown; // 구르기 가능 여부 확인
    }

    public void PerformRoll() // PlayerControllerFSM의 RollState에서 호출 (기존 OnRoll에서 이름 변경 일관성)
    {
        lastRollTime = Time.time; // 마지막 구르기 시간 기록
    }

    public void SetInvincibility(bool invincible)
    {
        // 구르기 중 무적 레이어 변경
        gameObject.layer = LayerMask.NameToLayer(invincible ? "PlayerInvincible" : "Player"); // 무적 상태에 따른 레이어 변경
    }

    private void HandleAimUI()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // 마우스 위치를 월드 좌표로 변환
        mouseWorldPosition.z = 0f; // 2D 게임이므로 z=0
        aimUIInstance.transform.position = mouseWorldPosition; // 조준 UI 위치 업데이트
    }

    private void HandleZoom()
    {
        if (virtualCamera == null) return;
        
    }

    public void SetCameraZoom(float targetSize, float duration)
    {
        if (virtualCamera == null) return;
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(SmoothZoom(virtualCamera.m_Lens.OrthographicSize, targetSize, duration));
    }

    public void ForceCameraZoomOutOnHit() // 메서드 이름 명확화
    {
        SetCameraZoom(zoomOutSize, zoomTransitionDuration);
    }

    private IEnumerator SmoothZoom(float startSize, float endSize, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(startSize, endSize, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        virtualCamera.m_Lens.OrthographicSize = endSize; // 정확히 목표 크기로 설정
        zoomCoroutine = null; // 코루틴 완료 후 null로 설정
    }


    public void FireArrow()
    {
        if (arrowPrefab == null || firePoint == null || aimUIInstance == null) 
        {
            Debug.LogError("화살 프리팹 참조 필요"); 
            return;
        }

        Vector2 aimPosition = aimUIInstance.transform.position; 
        Vector2 direction = (aimPosition - (Vector2)firePoint.position).normalized; 

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; 
        float referenceAngle = Movement.IsFacingRight ? 0f : 180f; 
        float relativeAngle = Mathf.Abs(Mathf.DeltaAngle(angle, referenceAngle)); 

        if (relativeAngle <= maxFireAngle) 
        {
            Vector2 spawnPosition = firePoint.position; 
            GameObject arrowInstance = Instantiate(arrowPrefab, spawnPosition, Quaternion.identity); 

            Projectile arrowScript = arrowInstance.GetComponent<Projectile>(); 
            if (arrowScript != null) 
            {
                float finalArrowDamage = Status.ATK.Value * arrowDamageMultiplier; // 플레이어 기본 공격력 * 배율
                arrowScript.Initialize(direction, finalArrowDamage, gameObject); // Projectile 초기화 (데미지, 발사자 정보 전달)
                Debug.Log($"플레이어 화살 발사 방향: {direction}, 기본 데미지: {Status.ATK.Value}, 배율: {arrowDamageMultiplier}, 최종 데미지: {finalArrowDamage}");
            }
        }
    }

    private void HandleAngleIndicator()
    {
        if (!(stateMachine.CurrentState is AttackState attackState && attackState.IsWaitingForFire())) // AttackState의 IsWaitingForFire 사용
        {
            if (angleIndicatorRenderer.enabled) angleIndicatorRenderer.enabled = false;
            return;
        }
        angleIndicatorRenderer.enabled = true; // 조건 만족 시 활성화

        Vector2 firePointPos = firePoint.position; // 발사 지점
        float characterDirectionAngle = Movement.IsFacingRight ? 0f : 180f; // 캐릭터 방향

        float angleMinRad = (characterDirectionAngle - maxFireAngle) * Mathf.Deg2Rad; // 최소 각도
        float angleMaxRad = (characterDirectionAngle + maxFireAngle) * Mathf.Deg2Rad; // 최대 각도

        Vector2 dirMin = new Vector2(Mathf.Cos(angleMinRad), Mathf.Sin(angleMinRad)); // 최소 각도 방향 벡터
        Vector2 dirMax = new Vector2(Mathf.Cos(angleMaxRad), Mathf.Sin(angleMaxRad)); // 최대 각도 방향 벡터

        angleIndicatorRenderer.positionCount = 3; // LineRenderer 정점 개수
        angleIndicatorRenderer.SetPosition(0, firePointPos + dirMin * angleIndicatorLength); // 첫 번째 정점
        angleIndicatorRenderer.SetPosition(1, firePointPos); // 두 번째 정점 (중심)
        angleIndicatorRenderer.SetPosition(2, firePointPos + dirMax * angleIndicatorLength); // 세 번째 정점

        Vector2 aimPosition = aimUIInstance.transform.position; // 조준 위치
        Vector2 currentShotDirection = (aimPosition - firePointPos).normalized; // 현재 조준 방향
        float currentShotAngleDegrees = Mathf.Atan2(currentShotDirection.y, currentShotDirection.x) * Mathf.Rad2Deg; // 현재 조준 각도
        float relativeAngle = Mathf.Abs(Mathf.DeltaAngle(currentShotAngleDegrees, characterDirectionAngle)); // 상대 각도

        angleIndicatorRenderer.startColor = (relativeAngle <= maxFireAngle) ? validAngleColor : invalidAngleColor; // 각도에 따른 색상 변경
        angleIndicatorRenderer.endColor = (relativeAngle <= maxFireAngle) ? validAngleColor : invalidAngleColor; // 각도에 따른 색상 변경
    }

    public bool CanMeleeAttack()
    {
        return Time.time >= lastMeleeAttackTime + meleeAttackCooldown; // 근접 공격 가능 여부
    }

    public void SetLastMeleeAttackTime()
    {
        lastMeleeAttackTime = Time.time;
    }

    public void PerformMeleeAttack()
    {
        lastMeleeAttackTime = Time.time; // 쿨타임 시작
        // 애니메이션 재생 및 데미지 판정은 MeleeAttackState에서 처리
    }

    public bool movementDisabled = false; 

    public void TemporarilyDisableMovement(float duration) 
    {
        StartCoroutine(DisableMovementRoutine(duration)); 
    }

    private IEnumerator DisableMovementRoutine(float duration) 
    {
        movementDisabled = true; 
        yield return new WaitForSeconds(duration); 
        movementDisabled = false; 
    }

    public void TakeDamage(float damageAmount)
    {
        if (Status.HP.Value <= 0 || stateMachine.CurrentState is RollState || stateMachine.CurrentState is PlayerDieState) // 이미 죽었거나, 구르기 중이거나, Die 상태면 무시
        {
            return;
        }

        Status.HP.Value -= damageAmount; 
        Debug.Log($"플레이어가 데미지 : {damageAmount} 받음. 현재 HP: {Status.HP.Value}"); 

        if (Status.HP.Value > 0) 
        {
            ChangeState(EPlayerState.Hit); // Hit 상태로 전환
            ForceCameraZoomOutOnHit();
        }
    }

    private void Die()
    {
        if (stateMachine.CurrentState is PlayerDieState) return; 

        Debug.Log("플레이어 사망"); 
        ChangeState(EPlayerState.Die);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameOverUI();
        }

        BossController boss = FindObjectOfType<BossController>(); 
        if (boss != null)
        {
            boss.NotifyPlayerDeath();
        }
    }

}