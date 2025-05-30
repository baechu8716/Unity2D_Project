using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus
{
    public ObservableProperty<int> ATK { get; private set; }
    public ObservableProperty<float> Health { get; private set; }
    public ObservableProperty<bool> IsGrounded { get; private set; }

    public PlayerStatus(float initialHealth, int initialATK) // 생성자에 공격력 초기값 추가
    {
        Health = new ObservableProperty<float>(initialHealth);
        IsGrounded = new ObservableProperty<bool>(true);
        ATK = new ObservableProperty<int>(initialATK); // 공격력 초기화
    }
}

