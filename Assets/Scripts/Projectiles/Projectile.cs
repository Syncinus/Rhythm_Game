using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float Speed;
    public float Direction;

    private new SpriteRenderer renderer;

    void Start()
    {
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, -Direction));
        renderer = GetComponent<SpriteRenderer>();
    }

    // Move the projectile
    void FixedUpdate()
    {
        transform.Translate(Vector2.up * Time.deltaTime * Speed * 10);
        if (!renderer.isVisible)
        {
            Destroy(gameObject);
        }
    }
}
