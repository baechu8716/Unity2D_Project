using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float _rollspeed = 2f; // 구르기 속도
    [SerializeField] private float moveSpeed = 5f; // 플레이어 속도
    [SerializeField] private float jumpForce = 5f; // 점프 힘
    [SerializeField] private float fallMultiplier = 2.5f; // 떨어질 때 더 빨리 떨어지도록
    [SerializeField] private float lowJumpMultiplier = 2f; // 낮은 점프 조정
    [SerializeField] private Transform groundCheck; // 지면 체크 포인트
    [SerializeField] private float groundRadius = 0.2f; // 지면 체크 반경
    [SerializeField] private LayerMask groundLayer; // 지면 레이어

    private bool isFacingRight = true; // 기본적으로 오른쪽을 향함
    public bool IsFacingRight => isFacingRight; // 기본 바라보는 방향이 오른쪽
    public float VerticalVelocity => rb != null ? rb.velocity.y : 0f; // rb null 체크 추가

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerMovement: Rigidbody2D component not found!");
        }
    }

    private void Update()
    {
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.movementDisabled) // 이 조건문이 Die 상태에서 true가 됨
        {
            return; // 이동 및 방향 전환 로직 실행 안 함
        }

        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput != 0)
        {
            isFacingRight = moveInput > 0;
            transform.localScale = new Vector3(isFacingRight ? 1f : -1f, 1f, 1f);
        }
    }

    public void Move(float direction)
    {
        if (rb == null) return;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.movementDisabled) // 이 조건문이 Die 상태에서 true가 됨
        {
            return; // 이동 로직 실행 안 함
        }

        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
    }

    public void Jump()
    {
        if (rb == null) return;
        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    public void Roll()
    {
        if (rb == null) return;
        if (IsGrounded())
        {
            float rollSpeed = moveSpeed * _rollspeed;
            // isFacingRight는 Update에서 갱신되므로 그 값을 사용
            rb.velocity = new Vector2(isFacingRight ? rollSpeed : -rollSpeed, rb.velocity.y);
        }
    }

    public bool IsGrounded()
    {
        if (groundCheck == null) return false;

        // OverlapCircle로 지면 체크
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        if (hit == null)
        {
            // 지면이 아닐 때는 false 반환
            return false;
        }

        // “지면과 접촉”이 감지되었다면, 
        // Y속도가 아주 작은 음수(또는 0)이 아니면 강제로 0으로 만들어서 틈새로 뚫고 내려가는 걸 방지
        if (rb.velocity.y < 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
        }

        return true;
    }

    // 추가된 메서드: 움직임을 즉시 멈춤
    public void StopImmediately()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // 자연스러운 점프와 낙하를 위한 중력 조절
        if (rb.velocity.y < 0) // 떨어질 때
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump")) // 점프 키를 짧게 눌렀을 때 (낮은 점프)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundRadius); // 원거리 공격 거리 시각화
    }
}
