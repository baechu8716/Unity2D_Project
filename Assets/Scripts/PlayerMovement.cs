using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float fallMultiplier = 2.5f; // 떨어질 때 더 빨리 떨어지도록
    [SerializeField] private float lowJumpMultiplier = 2f; // 낮은 점프 조정
    [SerializeField] private Transform groundCheck; // 지면 체크 포인트
    [SerializeField] private float groundRadius = 0.2f; // 지면 체크 반경
    [SerializeField] private LayerMask groundLayer; // 지면 레이어

    private bool isFacingRight = true; // 기본적으로 오른쪽을 향함
    public bool IsFacingRight => isFacingRight;
    public float VerticalVelocity => rb.velocity.y;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (groundCheck == null)
        {

        }
    }

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
        {
            isFacingRight = moveInput > 0;
            transform.localScale = new Vector3(isFacingRight ? 1f : -1f, 1f, 1f);
        }
    }

    public void Move(float direction)
    {
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
    }

    public void Jump()
    {
        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
        else
        {
        }
    }

    public void Roll()
    {
        if (IsGrounded())
        {
            float rollSpeed = moveSpeed * 1.5f;
            rb.velocity = new Vector2(isFacingRight ? rollSpeed : -rollSpeed, rb.velocity.y);
        }
    }

    public bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }
        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        return grounded;
    }

    void FixedUpdate()
    {
        // 자연스러운 점프와 낙하
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }
}