using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerFSM : MonoBehaviour
{
    public class PlayerControllerFSM : MonoBehaviour
    {
        // ������Ʈ ����
        [Header("Core")]
        public PlayerStatus Status;
        public PlayerMovement Movement;
        public Animator Anim;
        public Rigidbody2D Rb;

        [Header("Aim UI & Camera")]
        public GameObject AimUIPrefab;
        private GameObject _aimUI;
        public CinemachineVirtualCamera VCam;
        private float _defaultOrtho;
        public float AimZoomOrtho = 3f;
        public float ZoomSpeed = 10f;

        // FSM
        private StateMachine _sm;
        public EPlayerState CurState { get; private set; }
        private readonly int HASH_SPEED = Animator.StringToHash("Speed");
        private readonly int HASH_VSPEED = Animator.StringToHash("VerticalSpeed");
        private readonly int HASH_GROUND = Animator.StringToHash("IsGrounded");

        // �Է�
        private float inputX;
        private bool jumpWants, rollWants, aimHeld, attackWants;

        void Awake()
        {
            // �⺻ ������Ʈ
            Rb = GetComponent<Rigidbody2D>();
            Movement = GetComponent<PlayerMovement>();
            Status = GetComponent<PlayerStatus>();
            Anim = GetComponentInChildren<Animator>();

            // Aim UI
            _aimUI = Instantiate(AimUIPrefab, transform.parent);
            _aimUI.SetActive(false);

            // �ʱ� ī�޶� ������ ����
            _defaultOrtho = VCam.m_Lens.OrthographicSize;

            // FSM ����
            _sm = new StateMachine();
            _sm.AddState(EPlayerState.Idle, OnEnterIdle, OnUpdateIdle, OnExitIdle);
            _sm.AddState(EPlayerState.Move, OnEnterMove, OnUpdateMove, OnExitMove);
            _sm.AddState(EPlayerState.Jump, OnEnterJump, OnUpdateJump, OnExitJump);
            _sm.AddState(EPlayerState.Fall, OnEnterFall, OnUpdateFall, OnExitFall);
            _sm.AddState(EPlayerState.Roll, OnEnterRoll, OnUpdateRoll, OnExitRoll);
            _sm.AddState(EPlayerState.Aim, OnEnterAim, OnUpdateAim, OnExitAim);
            _sm.AddState(EPlayerState.Attack, OnEnterAtk, OnUpdateAtk, OnExitAtk);

            _sm.ChangeState(EPlayerState.Idle);

            // ����: ���� �浹
            Status.IsGrounded.Subscribe(g =>
            {
                Anim.SetBool(HASH_GROUND, g);
                if (g && _sm.Cur == EPlayerState.Fall)
                    _sm.ChangeState(EPlayerState.Idle);
            });
        }

        void Update()
        {
            // �Է� ����
            inputX = Input.GetAxisRaw("Horizontal");
            jumpWants = Input.GetKeyDown(KeyCode.Space);
            rollWants = Input.GetKeyDown(KeyCode.LeftShift);
            aimHeld = Input.GetMouseButton(1);
            attackWants = aimHeld && Input.GetMouseButtonDown(0);

            // FSM Update
            _sm.Cur.HandleInput();
            _sm.Cur.LogicUpdate();

            // Aim UI ��ġ ����
            if (aimHeld)
            {
                Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(r, out var hit, 100f))
                {
                    _aimUI.transform.position = hit.point;
                }
            }

            // Cinemachine Zoom
            float targetOrtho = aimHeld ? AimZoomOrtho : _defaultOrtho;
            VCam.m_Lens.OrthographicSize = Mathf.Lerp(
                VCam.m_Lens.OrthographicSize, targetOrtho, Time.deltaTime * ZoomSpeed);
        }

        void FixedUpdate()
        {
            // ���� ������ ���¿� ����
            _sm.Cur.PhysicsUpdate();
            Movement.AdjustGravity();
            Status.IsGrounded.Value = Mathf.Abs(Rb.velocity.y) < 0.05f;
        }

        // ������ ���� ���� ������������������������������������������������������������
        #region Idle
        void OnEnterIdle()
        {
            CurState = EPlayerState.Idle;
            Anim.Play("Idle");
        }
        void OnUpdateIdle()
        {
            if (Mathf.Abs(inputX) > 0) _sm.ChangeState(EPlayerState.Move);
            else if (jumpWants) _sm.ChangeState(EPlayerState.Jump);
            else if (rollWants) _sm.ChangeState(EPlayerState.Roll);
            else if (aimHeld) _sm.ChangeState(EPlayerState.Aim);
        }
        void OnExitIdle() { }
        #endregion

        #region Move
        void OnEnterMove()
        {
            CurState = EPlayerState.Move;
            Anim.Play("Walk");
        }
        void OnUpdateMove()
        {
            Movement.Move(inputX);
            if (Mathf.Approximately(inputX, 0f)) _sm.ChangeState(EPlayerState.Idle);
            else if (jumpWants) _sm.ChangeState(EPlayerState.Jump);
            else if (rollWants) _sm.ChangeState(EPlayerState.Roll);
            else if (aimHeld) _sm.ChangeState(EPlayerState.Aim);

            Anim.SetFloat(HASH_SPEED, Mathf.Abs(Rb.velocity.x));
        }
        void OnExitMove() { }
        #endregion

        #region Jump
        void OnEnterJump()
        {
            CurState = EPlayerState.Jump;
            Movement.Jump();
            Anim.Play("JumpUp");
        }
        void OnUpdateJump()
        {
            if (Rb.velocity.y < 0) _sm.ChangeState(EPlayerState.Fall);
            Anim.SetFloat(HASH_VSPEED, Rb.velocity.y);
        }
        void OnExitJump() { }
        #endregion

        #region Fall
        void OnEnterFall()
        {
            CurState = EPlayerState.Fall;
            Anim.Play("JumpDown");
        }
        void OnUpdateFall()
        {
            Anim.SetFloat(HASH_VSPEED, Rb.velocity.y);
        }
        void OnExitFall() { }
        #endregion

        #region Roll
        void OnEnterRoll()
        {
            CurState = EPlayerState.Roll;
            Anim.Play("Roll");
            float dir = inputX != 0 ? Mathf.Sign(inputX) : transform.localScale.x;
            Movement.Roll(dir);
        }
        void OnUpdateRoll()
        {
            // Roll �ִ� ������ Idle
            if (!Anim.GetCurrentAnimatorStateInfo(0).IsName("Roll"))
                _sm.ChangeState(EPlayerState.Idle);
        }
        void OnExitRoll() { }
        #endregion

        #region Aim
        void OnEnterAim()
        {
            CurState = EPlayerState.Aim;
            Anim.Play("AimIdle");
            _aimUI.SetActive(true);
        }
        void OnUpdateAim()
        {
            if (!aimHeld) _sm.ChangeState(EPlayerState.Idle);
            else if (attackWants) _sm.ChangeState(EPlayerState.Attack);
        }
        void OnExitAim()
        {
            _aimUI.SetActive(false);
        }
        #endregion

        #region Attack
        void OnEnterAtk()
        {
            CurState = EPlayerState.Attack;
            Anim.Play("Attack");
            // TODO: Bow.Shoot() ȣ��
        }
        void OnUpdateAtk()
        {
            if (!Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                _sm.ChangeState(EPlayerState.Aim);
        }
        void OnExitAtk() { }
        #endregion
    }

    
}
