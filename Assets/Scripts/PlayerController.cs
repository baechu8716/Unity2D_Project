using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(PlayerStatus), typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    private PlayerStatus _status;
    private PlayerMovement _movement;
    private Animator _animator;
    //[SerializeField] private Image _hpBar;

    private void Awake()
    {
        _status = GetComponent<PlayerStatus>();
        _movement = GetComponent<PlayerMovement>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        _status.IsMoving.Subscribe(OnMoving);
        _status.IsJumping.Subscribe(OnJumping);
        _status.IsDashing.Subscribe(OnDashing);
        _status.IsAiming.Subscribe(OnAiming);
        _status.IsAttacking.Subscribe(OnAttacking);
        //_status.CurrentHP.Subscribe(OnHPChanged);
    }

    private void OnDisable()
    {
        _status.IsMoving.Unsubscribe(OnMoving);
        _status.IsJumping.Unsubscribe(OnJumping);
        _status.IsDashing.Unsubscribe(OnDashing);
        _status.IsAiming.Unsubscribe(OnAiming);
        _status.IsAttacking.Unsubscribe(OnAttacking);
        //_status.CurrentHP.Unsubscribe(OnHPChanged);
    }

    private void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        _movement.Move(inputX);
        if (inputX != 0)
            transform.localScale = new Vector3(Mathf.Sign(inputX), 1, 1);

        if (Input.GetKeyDown(KeyCode.Space))
            _movement.Jump();

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            float dir = (inputX != 0) ? Mathf.Sign(inputX) : transform.localScale.x;
            _movement.Dash(dir);
        }
        // 4) 조준 모드 (우클릭 토글)
        if (Input.GetMouseButtonDown(1))
            _status.IsAiming.Value = true;
        if (Input.GetMouseButtonUp(1))
            _status.IsAiming.Value = false;

        // 5) 발사 (좌클릭)
        if (_status.IsAiming.Value && Input.GetMouseButton(0))
            _status.IsAttacking.Value = true;
        else
            _status.IsAttacking.Value = false;
    }


    // ─── 상태 구독 콜백 ──────────────────────────
    private void OnMoving(bool value) =>  _animator.SetBool("IsMove", value);
    private void OnJumping(bool value) => _animator.SetTrigger("Jump");
    private void OnDashing(bool value) => _animator.SetTrigger("Dash");
    private void OnAiming(bool value) =>  _animator.SetBool("IsAim", value);
    private void OnAttacking(bool value) => _animator.SetTrigger("Attack");

    // ─── HP UI 갱신 ──────────────────────────
    //private void OnHPChanged(int hp)
    //{
    //    _hpBar.fillAmount = hp / (float)_status.MaxHP;
    //}
}
