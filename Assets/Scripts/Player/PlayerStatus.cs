using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus
{
    public ObservableProperty<float> HP { get; private set; } // 기존 Health에서 HP로 변경 (일관성)
    public ObservableProperty<int> ATK { get; private set; }

    // 생성자에서 ATK 초기화 추가
    public PlayerStatus(float initialHp, int initialAtk) // PlayerController.cs의 Awake와 일치시킴
    {
        HP = new ObservableProperty<float>(initialHp);
        ATK = new ObservableProperty<int>(initialAtk);
    }
}

