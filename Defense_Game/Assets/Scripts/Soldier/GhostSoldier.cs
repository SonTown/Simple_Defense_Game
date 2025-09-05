using UnityEngine;

public class GhostSoldier : MonoBehaviour
{
    public void Setup(Vector3 pos, Quaternion rot) {
        transform.position = pos;
        transform.rotation = rot;
    }
}