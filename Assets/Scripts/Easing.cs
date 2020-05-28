using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public static class EasingFunctions
{
    public static float Linear(float V)
    {
        return V;
    }
}

public struct Easing : ILevelParsable
{
    public string Type;
    public float Duration;

    public Easing(string Type_, float Duration_)
    {
        Type = Type_;
        Duration = Duration_;
    }

    void ILevelParsable.Parse(string Info, Dictionary<string, string> Variables, ref bool IsDynamic)
    {
        string[] Arguments = Info.Split(',');
        Debug.Log(String.Join(",", Arguments));
        Type = Arguments[0];
        Duration = (float)LevelController.StringCast(typeof(float), Arguments[1], Variables, out IsDynamic);
    }

    public void Run(Action<float> Execute)
    {
        if (Duration > 0)
        {
            LevelController.Instance.StartCoroutine(_Run(Execute));
        } else
        {
            Execute(1);
        }
    }

    private IEnumerator _Run(Action<float> Execute) {
        float Timer = 0f;
        while (Timer < Duration)
        {
            float Progress = Mathf.Min(Timer / Duration, 1);
            float Value = Progress;
            if (Type.ToLower() == "linear")
            {
                Value = EasingFunctions.Linear(Progress);
            }
            Execute(Value);
            Timer += Time.deltaTime;
            // Make sure we always execute with 1
            if (Progress < 1 && Timer > Duration)
            {
                Execute(1);
            }
            yield return null;
        }
        
    }
}
