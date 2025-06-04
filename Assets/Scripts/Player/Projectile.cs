using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f; // 투사체 속도
    [SerializeField] private float lifetime = 3f; // 투사체 생존 시간 (3초 후 자동 파괴)

    private Rigidbody2D rb;
    private float currentDamage;
    private GameObject owner; // 발사 주체

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); //
        Destroy(gameObject, lifetime); // 설정된 lifetime 후 자동 파괴
    }

    public void Initialize(Vector2 direction, float damage, GameObject projectileOwner)
    {
        this.currentDamage = damage; 
        this.owner = projectileOwner; 

        if (rb != null)
        {
            rb.velocity = direction.normalized * speed; 
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; 
        transform.rotation = Quaternion.Euler(0f, 0f, angle); 
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return; // 자기 자신 또는 발사자와의 충돌 무시

        // IDamageable 인터페이스를 가진 대상에게만 데미지를 입힘
        IDamageable damageableTarget = other.GetComponent<IDamageable>();
        if (damageableTarget != null)
        {
            bool canDamage = false;
            // 플레이어가 발사한 화살은 "Boss" 태그를 가진 대상에게만 데미지를 입힘
            if (owner != null && owner.CompareTag("Player") && other.CompareTag("Boss"))
            {
                canDamage = true;
            }

            if (canDamage)
            {
                damageableTarget.TakeDamage(currentDamage); 
                Debug.Log($"{owner.name}의 투사체가 {other.name}에게 {currentDamage}의 데미지를 입혔습니다.");
                Destroy(gameObject); // 대상에게 명중 시 즉시 파괴
                return; // 파괴 후 추가 로직 실행 방지
            }
        }
    }
}