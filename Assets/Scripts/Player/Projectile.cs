// Projectile.cs (Arrow.cs에서 이름 변경 또는 내용 수정)
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f; // 투사체 속도
    [SerializeField] private float lifetime = 5f; // 투사체 생존 시간
    [SerializeField] private float rotationOffset = 0f; // 스프라이트 회전 보정값

    private Rigidbody2D rb;
    private float currentDamage;
    private GameObject owner; // 발사 주체

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); // Rigidbody2D 컴포넌트 가져오기
        Destroy(gameObject, lifetime); // 일정 시간 후 자동 파괴
    }

    public void Initialize(Vector2 direction, float damage, GameObject projectileOwner)
    {
        this.currentDamage = damage; // 데미지 설정
        this.owner = projectileOwner; // 발사자 설정

        if (rb != null)
        {
            rb.velocity = direction.normalized * speed; // 속도와 방향 설정
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // 방향에 따른 각도 계산
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset); // 투사체 회전
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return; // 자기 자신 또는 발사자와의 충돌 무시

        IDamageable damageableTarget = other.GetComponent<IDamageable>();
        if (damageableTarget != null)
        {
            // 아군 오사 방지 로직 (선택적 강화)
            // 예: 플레이어가 발사한 투사체는 "Enemy" 태그/레이어에만 데미지
            //     보스가 발사한 투사체는 "Player" 태그/레이어에만 데미지
            bool canDamage = false;
            if (owner != null && owner.CompareTag("Player") && other.CompareTag("Boss")) // 또는 "Enemy"
            {
                canDamage = true;
            }
            else if (owner != null && owner.CompareTag("Boss") && other.CompareTag("Player"))
            {
                canDamage = true;
            }
            // 태그가 없다면, owner의 레이어와 other의 레이어를 비교하여 충돌 매트릭스에 따라 결정하도록 할 수도 있음

            if (canDamage) // 위의 조건을 만족하거나, 단순화된 로직에서는 이 if 없이 바로 데미지
            {
                damageableTarget.TakeDamage(currentDamage); // 대상에게 데미지 주기
                Debug.Log($"{owner.name}'s projectile hit {other.name} for {currentDamage} damage.");
            }
            else if (owner == null) // owner가 설정 안된 경우 (테스트 등)에는 일단 데미지
            {
                damageableTarget.TakeDamage(currentDamage);
                Debug.LogWarning($"Projectile from unknown owner hit {other.name} for {currentDamage} damage.");
            }


        }

        Destroy(gameObject); // 대부분의 경우 충돌 후 투사체 파괴
    }
}