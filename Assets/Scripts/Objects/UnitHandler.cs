using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHandler : MonoBehaviour
{
    public Player Parent;

    public void OnTriggerEnter2D(Collider2D Collide)
    {
        Parent.OnHit(Collide);
    }
}
