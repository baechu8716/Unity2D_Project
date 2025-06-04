using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossStatus
{
    public ObservableProperty<float> HP { get; private set; }
    public int ATK { get; private set; } // 보스 공격력은 Observable이 필수는 아닐 수 있음 (UI 표시 등이 없다면)
                                         // 하지만 일관성을 위해 Observable로 만들어도 무방. 여기서는 간단히 int로.

    public BossStatus(float initialHp, int initialAtk)
    {
        HP = new ObservableProperty<float>(initialHp);
        ATK = initialAtk;
    }
}
