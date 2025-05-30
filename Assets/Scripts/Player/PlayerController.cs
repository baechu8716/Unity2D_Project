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
    private float lastRollTime; // ������ ������ �ð�

    private float lastMeleeAttackTime;
    [SerializeField] private float meleeAttackCooldown = 1f; // MeleeAttackState�� ATTACK_COOLDOWN�� ����ȭ�ϰų� �� ������ ����

    [SerializeField] private GameObject aimUIPrefab;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float zoomInSize = 3f; // ���� �� Orthographic Size
    [SerializeField] private float zoomOutSize = 6f; // �ܾƿ� �� Orthographic Size
    [SerializeField] private float rollCooldown = 2f; // ������ ��Ÿ�� (2��)
    [SerializeField] private GameObject arrowPrefab; // ȭ�� ������
    [SerializeField] private Transform firePoint; // �߻� ��ġ (�÷��̾� ��ġ �Ǵ� ���� ��ġ)
    [SerializeField] private float maxAngle = 45f; // �߻� ������ �ִ� ���� (��: 45��)

    [SerializeField] private LineRenderer angleIndicatorRenderer; // Inspector���� �Ҵ��� LineRenderer
    [SerializeField] private float angleIndicatorLength = 1.5f; // ���� ���ü� ����
    [SerializeField] private Color validAngleColor = Color.green; // �߻� ���� ������ �� �� ����
    [SerializeField] private Color invalidAngleColor = Color.red; // �߻� �Ұ��� ������ �� �� ���� (���� UI�� ���� �� ���� �� ���)


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
            virtualCamera.m_Lens.OrthographicSize = zoomOutSize; // �ʱ� ũ�� ����

        lastRollTime = -rollCooldown; // �ʱⰪ ����
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
        // ������Ʈ ���ÿ��� Enemy���� �浹 ���� ����
    }

    public PlayerMovement Movement => movement;
    public PlayerStatus Status => status;
    public Animator Animator => animator;
    public bool HasPlayedJumpAnimation { get => hasPlayedJumpAnimation; set => hasPlayedJumpAnimation = value; }

    private void HandleAimUI()
    {
        // ���콺 ��ġ�� ���� ��ǥ�� ��ȯ
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f; // 2D�̹Ƿ� z���� 0���� ����
        aimUIInstance.transform.position = mouseWorldPosition; // AimUI�� ���콺 ��ġ�� �̵�
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

        // AttackState�� ��, �׸��� waitingForFire (���� �� ����) ������ ���� ǥ��
        bool isAiming = stateMachine.currentState is AttackState &&
                        (stateMachine.currentState as AttackState).IsWaitingForFire(); // AttackState�� IsWaitingForFire() �޼��� �ʿ�

        if (!isAiming && !(Input.GetMouseButton(1) && stateMachine.currentState is AttackState)) // AttackState ���� ���� ���� waitingForFire�� �ƴ� ���� ��Ŭ�� ������ ������ ǥ��
        {
            angleIndicatorRenderer.enabled = false;
            return;
        }
        angleIndicatorRenderer.enabled = true;


        Vector2 firePointPos = firePoint.position;
        float characterDirectionAngle = movement.IsFacingRight ? 0f : 180f; // ĳ���Ͱ� �ٶ󺸴� ���� ���� (��)

        // ��ä���� ���� �� ���� ���
        float angleMin = characterDirectionAngle - maxAngle;
        float angleMax = characterDirectionAngle + maxAngle;

        // ������ �������� ��ȯ
        float angleMinRad = angleMin * Mathf.Deg2Rad;
        float angleMaxRad = angleMax * Mathf.Deg2Rad;

        // ���� ���� ���
        Vector2 dirMin = new Vector2(Mathf.Cos(angleMinRad), Mathf.Sin(angleMinRad));
        Vector2 dirMax = new Vector2(Mathf.Cos(angleMaxRad), Mathf.Sin(angleMaxRad));

        // LineRenderer �� ���� (����1, �߻���, ����2)
        angleIndicatorRenderer.positionCount = 3;
        angleIndicatorRenderer.SetPosition(0, firePointPos + dirMin * angleIndicatorLength);
        angleIndicatorRenderer.SetPosition(1, firePointPos);
        angleIndicatorRenderer.SetPosition(2, firePointPos + dirMax * angleIndicatorLength);

        // ���� ���콺 ���� ������ ��ȿ���� Ȯ���ϰ� �� ���� ����
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