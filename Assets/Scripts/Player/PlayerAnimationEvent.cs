using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour
{
    public PlayerController playerController;

    public void FireArrow() => playerController?.FireArrow();

}
