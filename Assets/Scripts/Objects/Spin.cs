using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{

    public Transform Origin;
    public float MovementFactor;
    public float Speed;

    void FixedUpdate()
    {
        transform.RotateAround(Origin.position, new Vector3(0f, 0f, -1f), Speed * MovementFactor);   
    }
}
