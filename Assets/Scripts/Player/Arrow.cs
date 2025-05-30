using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float rotationOffset = 0f; // ��������Ʈ ���⿡ ���� ���� (��: �����̸� -90f)
    private Vector2 direction;
    private bool canCollideWithPlayer = false;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        GetComponent<Rigidbody2D>().velocity = direction * speed;

        // ���� ���͸� ������ ��ȯ
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !canCollideWithPlayer)
        {
            return;
        }
        Destroy(gameObject);
    }
}
