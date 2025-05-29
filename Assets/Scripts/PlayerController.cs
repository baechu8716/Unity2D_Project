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

    private GameObject aimUIInstance;
    private float lastRollTime; // 마지막 구르기 시간

    [SerializeField] private GameObject aimUIPrefab;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float zoomInSize = 3f; // 줌인 시 Orthographic Size
    [SerializeField] private float zoomOutSize = 6f; // 줌아웃 시 Orthographic Size
    [SerializeField] private float rollCooldown = 2f; // 구르기 쿨타임 (2초)
    [SerializeField] private GameObject arrowPrefab; // 화살 프리팹
    [SerializeField] private Transform firePoint; // 발사 위치 (플레이어 위치 또는 무기 위치)
    [SerializeField] private float maxAngle = 45f; // 발사 가능한 최대 각도 (예: 45도)


    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
        status = new PlayerStatus(100f);

        stateMachine = new StateMachine();
        stateMachine.AddState(EPlayerState.Idle, new IdleState(this));
        stateMachine.AddState(EPlayerState.Move, new MoveState(this));
        stateMachine.AddState(EPlayerState.Jump, new JumpState(this));
        stateMachine.AddState(EPlayerState.Fall, new FallState(this));
        stateMachine.AddState(EPlayerState.Roll, new RollState(this));

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
        HandleAttack();
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

    private void HandleAttack()
    {
        // 좌클릭
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 aimPosition = aimUIInstance.transform.position;
            Vector2 direction = (aimPosition - (Vector2)firePoint.position).normalized;

            // 발사 각도 계산
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // 플레이어 방향에 따라 기준 각도 조정
            float referenceAngle = movement.IsFacingRight ? 0f : 180f;
            float relativeAngle = Mathf.Abs(Mathf.DeltaAngle(angle, referenceAngle));

            // 각도 제한 확인
            if (relativeAngle <= maxAngle)
            {
                Vector2 spawnPosition = (Vector2)firePoint.position + direction * 0.5f;
                GameObject arrow = Instantiate(arrowPrefab, spawnPosition, Quaternion.identity);
                arrow.layer = LayerMask.NameToLayer("Projectile");
                arrow.GetComponent<Arrow>().SetDirection(direction);
            }
            else
            {
                Debug.Log("발사 불가");
            }
        }
    }
    
}
