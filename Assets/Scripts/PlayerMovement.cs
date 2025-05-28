using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float fallMultiplier = 2.5f; // ������ �� �� ���� ����������
    [SerializeField] private float lowJumpMultiplier = 2f; // ���� ���� ����

    private bool isFacingRight = true; // �⺻������ �������� ����
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
            // ��������Ʈ ���� ��ȯ ���� �߰� (��: transform.localScale ����)
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
        // ������ ���� (��: �ӵ� ����)
        rb.velocity = new Vector2(rb.velocity.x * 1.5f, rb.velocity.y);
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
}