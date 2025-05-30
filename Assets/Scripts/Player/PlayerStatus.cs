using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus
{
    public ObservableProperty<int> ATK { get; private set; }
    public ObservableProperty<float> Health { get; private set; }
    public ObservableProperty<bool> IsGrounded { get; private set; }

    public PlayerStatus(float initialHealth, int initialATK) // �����ڿ� ���ݷ� �ʱⰪ �߰�
    {
        Health = new ObservableProperty<float>(initialHealth);
        IsGrounded = new ObservableProperty<bool>(true);
        ATK = new ObservableProperty<int>(initialATK); // ���ݷ� �ʱ�ȭ
    }
}

