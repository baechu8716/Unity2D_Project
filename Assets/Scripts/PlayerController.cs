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
    private float lastRollTime; // ������ ������ �ð�

    [SerializeField] private GameObject aimUIPrefab;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float zoomInSize = 3f; // ���� �� Orthographic Size
    [SerializeField] private float zoomOutSize = 6f; // �ܾƿ� �� Orthographic Size
    [SerializeField] private float rollCooldown = 2f; // ������ ��Ÿ�� (2��)
    [SerializeField] private GameObject arrowPrefab; // ȭ�� ������
    [SerializeField] private Transform firePoint; // �߻� ��ġ (�÷��̾� ��ġ �Ǵ� ���� ��ġ)
    [SerializeField] private float maxAngle = 45f; // �߻� ������ �ִ� ���� (��: 45��)


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
            virtualCamera.m_Lens.OrthographicSize = zoomOutSize; // �ʱ� ũ�� ����

        lastRollTime = -rollCooldown; // �ʱⰪ ����
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

    private void HandleAttack()
    {
        // ��Ŭ��
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 aimPosition = aimUIInstance.transform.position;
            Vector2 direction = (aimPosition - (Vector2)firePoint.position).normalized;

            // �߻� ���� ���
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // �÷��̾� ���⿡ ���� ���� ���� ����
            float referenceAngle = movement.IsFacingRight ? 0f : 180f;
            float relativeAngle = Mathf.Abs(Mathf.DeltaAngle(angle, referenceAngle));

            // ���� ���� Ȯ��
            if (relativeAngle <= maxAngle)
            {
                Vector2 spawnPosition = (Vector2)firePoint.position + direction * 0.5f;
                GameObject arrow = Instantiate(arrowPrefab, spawnPosition, Quaternion.identity);
                arrow.layer = LayerMask.NameToLayer("Projectile");
                arrow.GetComponent<Arrow>().SetDirection(direction);
            }
            else
            {
                Debug.Log("�߻� �Ұ�");
            }
        }
    }
    
}
