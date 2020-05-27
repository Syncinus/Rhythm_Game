using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathClock : MonoBehaviour
{
    public float Lifetime = 0f;
    private float TimePassed = 0f;

    // Update is called once per frame
    void Update()
    {
        TimePassed += Time.deltaTime;
        if (TimePassed >= Lifetime)
        {
            Destroy(gameObject);
        }
    }
}
