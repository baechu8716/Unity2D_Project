using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private PlayerStatus _status;

    // 중력 보정 계수
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Move(float h)
    {
        float speed = _status.AimTriggered.Value
            ? _status.WalkSpeed
            : _status.RunSpeed;
        Vector2 v = _rb.velocity;
        v.x = h * speed;
        _rb.velocity = v;

        _status.MoveSpeed.Value = Mathf.Abs(v.x);
    }

    public void Jump()
    {
        if (_status.IsGrounded.Value)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, 0);
            _rb.AddForce(Vector2.up * _status.JumpForce, ForceMode2D.Impulse);
        }
    }

    public void Roll(float dir)
    {
        _rb.velocity = new Vector2(dir * _status.RollSpeed, _rb.velocity.y);
    }

    public void AdjustGravity()
    {
        if (_rb.velocity.y < 0)
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if (_rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;

        _status.VerticalSpeed.Value = _rb.velocity.y;
    }
}