using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shake
{
    public float Decay;
    public float CurrentIntensity;

    public Shake(float Decay, float Intensity)
    {
        this.Decay = Decay;
        this.CurrentIntensity = Intensity;
    }
};

public class Shaker : MonoBehaviour
{
    private Vector3 OriginPosition;
    private Quaternion OriginRotation;
    public List<Shake> Shakes = new List<Shake>();

    private void Awake()
    {
        OriginPosition = transform.position;
        OriginRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (Shakes.Count > 0)
        {
            float CurrentIntensity = 0f;
            for (int i = 0; i < Shakes.Count; i++)
            {
                CurrentIntensity += Shakes[i].CurrentIntensity;
                Shakes[i].CurrentIntensity -= Shakes[i].Decay;
            }
            if (CurrentIntensity > 0)
            {
                transform.position = OriginPosition + Random.insideUnitSphere * CurrentIntensity;
                transform.rotation = new Quaternion(
                    OriginRotation.x + Random.Range(-CurrentIntensity, CurrentIntensity) * 0.2f,
                    OriginRotation.y + Random.Range(-CurrentIntensity, CurrentIntensity) * 0.2f,
                    OriginRotation.z + Random.Range(-CurrentIntensity, CurrentIntensity) * 0.2f,
                    OriginRotation.w + Random.Range(-CurrentIntensity, CurrentIntensity) * 0.2f
                );
            }
            Shakes.RemoveAll(shake => shake.CurrentIntensity <= 0);
        } else
        {
            transform.position = OriginPosition;
            transform.rotation = OriginRotation;
        }
    }

    public void Stop()
    {
        Shakes.Clear();
    }

    public void Shake(float Decay, float Intensity)
    {
        Shakes.Add(new Shake(Decay, Intensity));
    }
}
