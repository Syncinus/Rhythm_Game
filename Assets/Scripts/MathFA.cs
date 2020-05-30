using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathFA
{
    public static float Lerp(float a, float b, float t)
    {
        float ShortestAngle = ((((b - a) % 360) + 540) % 360) - 180;
        return a + (ShortestAngle * t) % 360;
    }

    public static float Wrap(float x)
    {
        // TODO: FIND A BETTER FUNCTION FOR THIS
        return x - 360 * Mathf.Floor((x + 180) * (1 / 360));
    }
}