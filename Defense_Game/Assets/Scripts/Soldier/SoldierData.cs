using UnityEngine;
[CreateAssetMenu(fileName = "SoldierData", menuName = "RTS/SoldierData")]
public class SoldierData : ScriptableObject
{
    [Header("Data")]
    public int MaxHealth =100;
    public int Defense = 10;
    public float MoveSpeed = 10f;
}
