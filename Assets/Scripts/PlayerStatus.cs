using DesignPattern;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus
{
    public ObservableProperty<float> Health { get; private set; }
    public ObservableProperty<bool> IsGrounded { get; private set; }

    public PlayerStatus(float initialHealth)
    {
        Health = new ObservableProperty<float>(initialHealth);
        IsGrounded = new ObservableProperty<bool>(true);
    }
}

