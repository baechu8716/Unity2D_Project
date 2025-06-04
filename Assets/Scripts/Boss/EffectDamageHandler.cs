using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectDamageHandler : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("이펙트의 데미지 배율입니다. 최종 데미지 = 발사자 공격력 * 이 값")]
    [SerializeField] private float damageMultiplier = 1.0f; // 기본값 1.0 (공격력만큼)

    [Header("Lifetime & Targeting")]
    [Tooltip("이펙트가 자동으로 파괴되기까지의 시간입니다. 0이면 자동 파괴 안 함 (애니메이션 이벤트 등으로 파괴 권장).")]
    [SerializeField] private float lifetime = 3f;
    [Tooltip("이 이펙트가 데미지를 줄 수 있는 대상의 태그입니다.")]
    [SerializeField] private string targetTag = "Player";
    [Tooltip("데미지를 한 번만 줄지, 아니면 지속적으로 줄지 결정합니다 (장판 스킬 등에 사용).")]
    [SerializeField] private bool dealDamageOncePerTarget = true;
    [Tooltip("지속 데미지일 경우 데미지 간격 (초).")]
    [SerializeField] private float continuousDamageInterval = 0.5f;

    private float ownerAttackPower; // 발사 주체의 공격력 (외부에서 설정)
    private GameObject owner; // 이 이펙트를 발사한 게임 오브젝트
    private Collider2D col;
    private List<Collider2D> alreadyHitTargets;
    private float lastDamageTime;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"EffectDamageHandler on {gameObject.name} requires a Collider2D component.");
            enabled = false;
            return;
        }
        col.isTrigger = true;

        if (dealDamageOncePerTarget)
        {
            alreadyHitTargets = new List<Collider2D>();
        }

        if (lifetime > 0)
        {
            Destroy(gameObject, lifetime);
        }
    }

    public void Initialize(float attackPower, GameObject effectOwner, string targetTagToHit)
    {
        this.ownerAttackPower = attackPower;
        this.owner = effectOwner;
        this.targetTag = targetTagToHit;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!dealDamageOncePerTarget)
        {
            if (Time.time >= lastDamageTime + continuousDamageInterval)
            {
                HandleCollision(other);
                lastDamageTime = Time.time;
            }
        }
    }

    private void HandleCollision(Collider2D other)
    {
        if (owner != null && other.gameObject == owner)
        {
            return;
        }

        if (dealDamageOncePerTarget && alreadyHitTargets != null && alreadyHitTargets.Contains(other))
        {
            return;
        }

        if (other.CompareTag(targetTag))
        {
            IDamageable damageableTarget = other.GetComponent<IDamageable>();
            if (damageableTarget != null)
            {
                float finalDamage = ownerAttackPower * damageMultiplier; // 데미지 계산 방식 변경

                damageableTarget.TakeDamage(finalDamage);
                Debug.Log($"{gameObject.name} (owner: {(owner != null ? owner.name : "Unknown")}) dealt {finalDamage} damage to {other.gameObject.name}");

                if (dealDamageOncePerTarget && alreadyHitTargets != null)
                {
                    alreadyHitTargets.Add(other);
                }
            }
        }
    }

    public void DestroyEffectViaAnimation()
    {
        Destroy(gameObject);
    }
}
