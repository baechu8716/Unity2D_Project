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

    private bool isFacingRight = true; // 기본적으로 오른쪽을 향함
    public bool IsFacingRight => isFacingRight;
    public float VerticalVelocity => rb.velocity.y;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
        {
            isFacingRight = moveInput > 0;
            // 스프라이트 방향 전환 로직 추가 (예: transform.localScale 조정)
            transform.localScale = new Vector3(isFacingRight ? 1f : -1f, 1f, 1f);
        }
    }

    public void Move(float direction)
    {
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
    }

    public void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    public void Roll()
    {
        // 구르기 로직 (예: 속도 증가)
        rb.velocity = new Vector2(rb.velocity.x * 1.5f, rb.velocity.y);
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