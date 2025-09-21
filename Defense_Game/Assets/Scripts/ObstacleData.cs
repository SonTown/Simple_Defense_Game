using UnityEngine;

[System.Serializable]
public class ObstacleData
{
    public Vector3Int position;
    public Vector3Int size;
    public FlowField.CellType cellType;
    public bool destructible;

    public ObstacleData(Vector3Int pos, Vector3Int size, FlowField.CellType type, bool destructible)
    {
        this.position = pos;
        this.size = size;
        this.cellType = type;
        this.destructible = destructible;
    }
}

[System.Serializable]
public class ObstacleDataList
{
    public ObstacleData[] items;
    public ObstacleDataList(ObstacleData[] items) => this.items = items;
}