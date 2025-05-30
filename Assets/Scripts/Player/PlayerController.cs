using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private StateMachine stateMachine;
    private PlayerMovement movement;
    private PlayerStatus status;
    private Animator animator;
    private bool hasPlayedJumpAnimation = false;

    private Collider2D playerCollider;
    private GameObject aimUIInstance;
    private float lastRollTime; // 마지막 구르기 시간

    private float lastMeleeAttackTime;
    [SerializeField] private float meleeAttackCooldown = 1f; // MeleeAttackState의 ATTACK_COOLDOWN과 동기화하거나 한 곳에서 관리

    [SerializeField] private GameObject aimUIPrefab;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float zoomInSize = 3f; // 줌인 시 Orthographic Size
    [SerializeField] private float zoomOutSize = 6f; // 줌아웃 시 Orthographic Size
    [SerializeField] private float rollCooldown = 2f; // 구르기 쿨타임 (2초)
    [SerializeField] private GameObject arrowPrefab; // 화살 프리팹
    [SerializeField] private Transform firePoint; // 발사 위치 (플레이어 위치 또는 무기 위치)
    [SerializeField] private float maxAngle = 45f; // 발사 가능한 최대 각도 (예: 45도)

    [SerializeField] private LineRenderer angleIndicatorRenderer; // Inspector에서 할당할 LineRenderer
    [SerializeField] private float angleIndicatorLength = 1.5f; // 각도 지시선 길이
    [SerializeField] private Color validAngleColor = Color.green; // 발사 가능 각도일 때 선 색상
    [SerializeField] private Color invalidAngleColor = Color.red; // 발사 불가능 각도일 때 선 색상 (조준 UI에 따라 색 변경 시 사용)


    void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        movement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
        status = new PlayerStatus(100f, 10);

        stateMachine = new StateMachine();
        stateMachine.AddState(EPlayerState.Idle, new IdleState(this));
        stateMachine.AddState(EPlayerState.Move, new MoveState(this));
        stateMachine.AddState(EPlayerState.Jump, new JumpState(this));
        stateMachine.AddState(EPlayerState.Fall, new FallState(this));
        stateMachine.AddState(EPlayerState.Roll, new RollState(this));
        stateMachine.AddState(EPlayerState.Attack, new AttackState(this));
        stateMachine.AddState(EPlayerState.MeleeAttack, new MeleeAttackState(this));

        stateMachine.ChangeState(EPlayerState.Idle);

        aimUIInstance = Instantiate(aimUIPrefab, Vector3.zero, Quaternion.identity);

        if (virtualCamera != null)
            virtualCamera.m_Lens.OrthographicSize = zoomOutSize; // 초기 크기 설정

        lastRollTime = -rollCooldown; // 초기값 설정
    }

    void LateUpdate()
    {
        stateMachine.Update();
        HandleAimUI();
        HandleZoom();
        HandleAngleIndicator();
    }

    public void ChangeState(EPlayerState newState)
    {
        stateMachine.ChangeState(newState);
    }

    public bool CanRoll()
    {
        return Time.time >= lastRollTime + rollCooldown;
    }

    public void OnRoll()
    {
        lastRollTime = Time.time;
    }

    public void SetInvincibility(bool invincible)
    {
        gameObject.layer = LayerMask.NameToLayer(invincible ? "PlayerInvincible" : "Player");
        // 프로젝트 세팅에서 Enemy공격 충돌 해제 예정
    }

    public PlayerMovement Movement => movement;
    public PlayerStatus Status => status;
    public Animator Animator => animator;
    public bool HasPlayedJumpAnimation { get => hasPlayedJumpAnimation; set => hasPlayedJumpAnimation = value; }

    private void HandleAimUI()
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f; // 2D이므로 z축은 0으로 설정
        aimUIInstance.transform.position = mouseWorldPosition; // AimUI를 마우스 위치로 이동
    }

    private void HandleZoom()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Zoom In triggered");
            if (virtualCamera != null)
                virtualCamera.m_Lens.OrthographicSize = zoomInSize;
            else
                Debug.LogError("Virtual Camera is not assigned!");
        }
        else if (Input.GetMouseButtonUp(1))
        {
            Debug.Log("Zoom Out triggered");
            if (virtualCamera != null)
                virtualCamera.m_Lens.OrthographicSize = zoomOutSize;
            else
                Debug.LogError("Virtual Camera is not assigned!");
        }
    }

    public void FireArrow()
    {
        Vector2 aimPosition = aimUIInstance.transform.position;
        Vector2 direction = (aimPosition - (Vector2)firePoint.position).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float referenceAngle = movement.IsFacingRight ? 0f : 180f;
        float relativeAngle = Mathf.Abs(Mathf.DeltaAngle(angle, referenceAngle));

        if (relativeAngle <= maxAngle)
        {
            Vector2 spawnPosition = (Vector2)firePoint.position + direction * 0.5f;
            GameObject arrow = Instantiate(arrowPrefab, spawnPosition, Quaternion.identity);
            arrow.layer = LayerMask.NameToLayer("Projectile");
            arrow.GetComponent<Arrow>().SetDirection(direction);
        }
    }


    private void HandleAngleIndicator()
    {
        if (angleIndicatorRenderer == null || firePoint == null || aimUIInstance == null)
        {
            if (angleIndicatorRenderer != null) angleIndicatorRenderer.enabled = false;
            return;
        }

        // AttackState일 때, 그리고 waitingForFire (조준 중 멈춤) 상태일 때만 표시
        bool isAiming = stateMachine.currentState is AttackState &&
                        (stateMachine.currentState as AttackState).IsWaitingForFire(); // AttackState에 IsWaitingForFire() 메서드 필요

        if (!isAiming && !(Input.GetMouseButton(1) && stateMachine.currentState is AttackState)) // AttackState 진입 직후 아직 waitingForFire가 아닐 때도 우클릭 누르고 있으면 표시
        {
            angleIndicatorRenderer.enabled = false;
            return;
        }
        angleIndicatorRenderer.enabled = true;


        Vector2 firePointPos = firePoint.position;
        float characterDirectionAngle = movement.IsFacingRight ? 0f : 180f; // 캐릭터가 바라보는 기준 각도 (도)

        // 부채꼴의 양쪽 끝 각도 계산
        float angleMin = characterDirectionAngle - maxAngle;
        float angleMax = characterDirectionAngle + maxAngle;

        // 각도를 라디안으로 변환
        float angleMinRad = angleMin * Mathf.Deg2Rad;
        float angleMaxRad = angleMax * Mathf.Deg2Rad;

        // 방향 벡터 계산
        Vector2 dirMin = new Vector2(Mathf.Cos(angleMinRad), Mathf.Sin(angleMinRad));
        Vector2 dirMax = new Vector2(Mathf.Cos(angleMaxRad), Mathf.Sin(angleMaxRad));

        // LineRenderer 점 설정 (끝점1, 발사점, 끝점2)
        angleIndicatorRenderer.positionCount = 3;
        angleIndicatorRenderer.SetPosition(0, firePointPos + dirMin * angleIndicatorLength);
        angleIndicatorRenderer.SetPosition(1, firePointPos);
        angleIndicatorRenderer.SetPosition(2, firePointPos + dirMax * angleIndicatorLength);

        // 현재 마우스 조준 방향이 유효한지 확인하고 선 색상 변경
        Vector2 aimPosition = aimUIInstance.transform.position;
        Vector2 currentShotDirection = (aimPosition - firePointPos).normalized;
        float currentShotAngleDegrees = Mathf.Atan2(currentShotDirection.y, currentShotDirection.x) * Mathf.Rad2Deg;

        float relativeAngle = Mathf.Abs(Mathf.DeltaAngle(currentShotAngleDegrees, characterDirectionAngle));

        if (relativeAngle <= maxAngle)
        {
            angleIndicatorRenderer.startColor = validAngleColor;
            angleIndicatorRenderer.endColor = validAngleColor;
        }
        else
        {
            angleIndicatorRenderer.startColor = invalidAngleColor;
            angleIndicatorRenderer.endColor = invalidAngleColor;
        }
    }
    public bool CanMeleeAttack()
    {
        return Time.time >= lastMeleeAttackTime + meleeAttackCooldown;
    }

    public void SetLastMeleeAttackTime()
    {
        lastMeleeAttackTime = Time.time;
    }
}