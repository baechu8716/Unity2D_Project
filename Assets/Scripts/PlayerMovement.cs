using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerStatus))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rigid;
    private PlayerStatus _status;

    private bool _canDash = true;
    private float _dashCoolDown = 1f;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _status = GetComponent<PlayerStatus>();
    }

    public void Move(float input)
    {
        float speed = _status.IsAiming.Value ? _status.WalkSpeed : _status.RunSpeed;

        Vector2 velocity = _rigid.velocity;
        velocity.x = input * speed;
        _rigid.velocity = velocity;

        // input값이 0이라면 ismoving은 false
        _status.IsMoving.Value = !Mathf.Approximately(input, 0f);
    }

    public void Jump()
    {
        // isgrounded대신 y축 속도로 공중에서 점프 제한
        if (Mathf.Abs(_rigid.velocity.y) < 0.05f)
        {
            _rigid.AddForce(Vector2.up * _status.JumpForce, ForceMode2D.Impulse);
            _status.IsJumping.Value = true;
            // 점프를 뛴 순간만 점프상태(IsJumping.Value = true)
            Invoke(nameof(ResetJumpFlag), 0.2f);
        }
    }

    public void ResetJumpFlag()
    {
        _status.IsJumping.Value = false;
    }

    public void Dash(float direction)
    {
        if (!_canDash) return;
        _canDash = false;
        _status.IsDashing.Value = true;

        _rigid.velocity = new Vector2(direction * _status.DashSpeed, _rigid.velocity.y);
        Invoke(nameof(EndDash), 0.2f);
        Invoke(nameof(ResetDashCooldown), _dashCoolDown);
    }

    public void EndDash()
    {
        _status.IsDashing.Value = false;
    }

    public void ResetDashCooldown()
    {
        _canDash = true;
    }
}
