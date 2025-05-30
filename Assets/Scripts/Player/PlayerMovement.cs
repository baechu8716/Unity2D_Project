using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float _rollspeed = 2f; // ������ �ӵ�
    [SerializeField] private float moveSpeed = 5f; // �÷��̾� �ӵ�
    [SerializeField] private float jumpForce = 5f; // ���� ��
    [SerializeField] private float fallMultiplier = 2.5f; // ������ �� �� ���� ����������
    [SerializeField] private float lowJumpMultiplier = 2f; // ���� ���� ����
    [SerializeField] private Transform groundCheck; // ���� üũ ����Ʈ
    [SerializeField] private float groundRadius = 0.2f; // ���� üũ �ݰ�
    [SerializeField] private LayerMask groundLayer; // ���� ���̾�

    private bool isFacingRight = true; // �⺻������ �������� ����
    public bool IsFacingRight => isFacingRight; // �⺻ �ٶ󺸴� ������ ������
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
    }

    public void Roll()
    {
        if (IsGrounded())
        {
            float rollSpeed = moveSpeed * _rollspeed;
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
        // �ڿ������� ������ ����
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void OnDrawGizmos()
    {
        // ���� ���� ����� 
        if (groundCheck != null && groundRadius > 0)
        {
            Gizmos.color = Color.green; 
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius); // groundCheck �ֺ� �� �׸���
        }
    }
}