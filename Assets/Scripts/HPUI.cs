using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPUI : MonoBehaviour
{
    public Slider hpSlider; // 인스펙터에서 연결할 HP 슬라이더 UI
    public MonoBehaviour targetCharacterController; // 인스PECTOR에서 연결할 플레이어 또는 보스 컨트롤러

    private ObservableProperty<float> _targetHP;
    private float _initialMaxHP;

    void Start()
    {
        if (hpSlider == null)
        {
            Debug.LogError("CharacterHPUI: HP Slider가 할당되지 않았습니다!", this);
            enabled = false; // 슬라이더 없이는 작동 불가
            return;
        }

        if (targetCharacterController == null)
        {
            Debug.LogError("CharacterHPUI: Target Character Controller가 할당되지 않았습니다!", this);
            enabled = false; // 대상 캐릭터 없이는 작동 불가
            return;
        }

        if (targetCharacterController is PlayerController player) // PlayerController인 경우
        {
            if (player.Status != null && player.Status.HP != null) 
            {
                _targetHP = player.Status.HP; 
                _initialMaxHP = player.Status.HP.Value; // 초기 HP를 최대 HP로 가정 (또는 PlayerStatus에 MaxHP 필드가 있다면 그것을 사용)
                Debug.Log($"PlayerHPUI: 플레이어 HP 초기화. MaxHP: {_initialMaxHP}, CurrentHP: {_targetHP.Value}", this);
            }
            else
            {
                Debug.LogError("CharacterHPUI: PlayerController의 Status 또는 HP를 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }
        }
        else if (targetCharacterController is BossController boss) // BossController인 경우
        {
            if (boss.Status != null && boss.Status.HP != null) // Status와 HP가 null이 아닌지 확인
            {
                _targetHP = boss.Status.HP; 
                _initialMaxHP = boss.Status.HP.Value; // 초기 HP를 최대 HP로 가정 (또는 BossStatus에 MaxHP 필드가 있다면 그것을 사용)
                Debug.Log($"CharacterHPUI: 보스 HP 초기화. MaxHP: {_initialMaxHP}, CurrentHP: {_targetHP.Value}", this);
            }
            else
            {
                Debug.LogError("CharacterHPUI: BossController의 Status 또는 HP를 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("CharacterHPUI: 지원하지 않는 캐릭터 컨트롤러 타입입니다.", this);
            enabled = false;
            return;
        }

        // 초기 HP 설정
        hpSlider.maxValue = _initialMaxHP;
        hpSlider.value = _targetHP.Value;

        // HP 변경 시 슬라이더 업데이트 이벤트 구독
        _targetHP.OnValueChanged += UpdateHPSlider;
    }

    void OnDestroy()
    {
        // 스크립트 파괴 시 이벤트 구독 해제
        if (_targetHP != null)
        {
            _targetHP.OnValueChanged -= UpdateHPSlider;
        }
    }

    private void UpdateHPSlider(float newHP)
    {
        if (hpSlider != null)
        {
            hpSlider.value = newHP;
        }
    }
}
