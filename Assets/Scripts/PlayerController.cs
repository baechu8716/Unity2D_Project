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

    [SerializeField] private GameObject aimUIPrefab;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float zoomInSize = 3f; // ���� �� Orthographic Size
    [SerializeField] private float zoomOutSize = 6f; // �ܾƿ� �� Orthographic Size
    private GameObject aimUIInstance;

    [SerializeField] private GameObject arrowPrefab; // ȭ�� ������
    [SerializeField] private Transform firePoint; // �߻� ��ġ (�÷��̾� ��ġ �Ǵ� ���� ��ġ)

    private void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0)) // ��Ŭ��
        {
            Vector2 aimPosition = aimUIInstance.transform.position;
            Vector2 direction = (aimPosition - (Vector2)firePoint.position).normalized;

            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
            arrow.GetComponent<Arrow>().SetDirection(direction);
        }
    }

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
        aimUIInstance.layer = LayerMask.NameToLayer("Ignore Raycast");

        if (virtualCamera != null)
            virtualCamera.m_Lens.OrthographicSize = zoomOutSize; // �ʱ� ũ�� ����
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
}
