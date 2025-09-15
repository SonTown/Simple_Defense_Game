using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Grid Data")]
    public Vector2Int size = new Vector2Int(1, 5); 
    public Vector2Int origin;  

    [Header("Properties")]
    public FlowField.CellType cellType = FlowField.CellType.Obstacle;
    public bool destructible = true;

    public ObstacleData ToData()
    {
        return new ObstacleData(origin, size, cellType, destructible);
    }

    public void FromData(ObstacleData data)
    {
        origin = data.position;
        size = data.size;
        cellType = data.cellType;
        destructible = data.destructible;

        transform.position = new Vector3(origin.x, 0, origin.y);
    }
}