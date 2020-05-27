using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

struct PlayerUnit
{
    public GameObject Unit;
    public float OffsetAngle;
    public PlayerUnit(GameObject Unit_, float OffsetAngle_)
    {
        Unit = Unit_;
        OffsetAngle = OffsetAngle_;
    }
}

public class Player : MonoBehaviour
{
    #region Variables
    public static float Angle = 0;

    public float Speed = 1f;
    public int TimesHit;

    private bool SwitchButtonPressed = false;
    #endregion

    #region Resources
    public Transform Origin;

    private Transform PlayerPrefab;
    private const float Distance = 16.8f;
    private Vector2 Size = new Vector2(0.5f, 0.5f);
    private Color CurrentColor = Color.black;
    #endregion

    private readonly List<PlayerUnit> Units = new List<PlayerUnit>();

    public void ChangeColor(Color color)
    {
        CurrentColor = color;
        for (int i = 0; i < Units.Count; i++)
        {
            Units[i].Unit.GetComponent<SpriteRenderer>().color = color;
        }
    }
    public void SetUnitCount(int Count)
    {
        // Iterate the existing units and destroy them
        for (int i = 0; i < Units.Count; i++)
        {
            Destroy(Units[i].Unit);
        }
        Units.Clear();
        // Create new units
        float AngleIncrement = 360f / Count;
        for (int i = 0; i < Count; i++)
        {
            float UnitAngle = i * AngleIncrement;
            Transform UnitObject = Instantiate(PlayerPrefab, transform);
            UnitObject.gameObject.layer = 8;
            UnitObject.localScale = Size;
            UnitObject.GetComponent<SpriteRenderer>().sortingOrder = 3;
            UnitObject.GetComponent<SpriteRenderer>().color = CurrentColor;
            Units.Add(new PlayerUnit(UnitObject.gameObject, UnitAngle));
        }
    }

    void UpdateUnits()
    {
        for (int i = 0; i < Units.Count; i++)
        {
            // Position the unit
            float UnitAngle = -(Angle + Units[i].OffsetAngle - 90) * Mathf.Deg2Rad;
            Vector3 DirectionVector = new Vector3(Mathf.Cos(UnitAngle), Mathf.Sin(UnitAngle), 0);
            Units[i].Unit.transform.position = Vector3.zero + (DirectionVector.normalized * Distance);
            // Make it look at the core
            Vector3 Difference = Origin.position - Units[i].Unit.transform.position;
            float LookAngle = Mathf.Atan2(Difference.y, Difference.x) * Mathf.Rad2Deg;
            Units[i].Unit.transform.rotation = Quaternion.Euler(0, 0, LookAngle - 90);
        }
    }

    void Awake()
    {
        // Load resources
        PlayerPrefab = Resources.Load<Transform>("Player");
    }

    void Start()
    {
        SetUnitCount(1);
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.A))
        {
            Angle += Speed * Time.fixedDeltaTime * 50;
        }
        if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.D))
        {
            Angle -= Speed * Time.fixedDeltaTime * 50;
        }
        if (Input.GetMouseButton(2) || Input.GetKey(KeyCode.W))
        {
            if (!SwitchButtonPressed)
            {
                Angle += 180;
            }
            SwitchButtonPressed = true;
        } else
        {
            SwitchButtonPressed = false;
        }
        UpdateUnits();
    }

}
