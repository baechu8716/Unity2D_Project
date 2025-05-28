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
    [SerializeField] private LayerMask aimLayerMask;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float zoomInFOV = 30f;
    [SerializeField] private float zoomOutFOV = 60f;
    private GameObject aimUIInstance;

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

        // AimUI 인스턴스 생성
        aimUIInstance = Instantiate(aimUIPrefab, Vector3.zero, Quaternion.identity);
        aimUIInstance.layer = LayerMask.NameToLayer("Ignore Raycast"); // AimUI 레이어를 Ignore Raycast로 설정
    }

    void Update()
    {
        stateMachine.Update();
        HandleAimUI();
        HandleZoom();
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics2D.Raycast(ray.origin, ray.direction, 100f, aimLayerMask))
        {
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100f, aimLayerMask);
            aimUIInstance.transform.position = hit.point;
        }
    }

    private void HandleZoom()
    {
        if (Input.GetMouseButtonDown(1)) // 우클릭
        {
            virtualCamera.m_Lens.FieldOfView = zoomInFOV;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            virtualCamera.m_Lens.FieldOfView = zoomOutFOV;
        }
    }
}
