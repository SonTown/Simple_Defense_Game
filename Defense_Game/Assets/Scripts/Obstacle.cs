using System;
using Unity.Entities;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    //Entity Info
    private Entity soldierEntity;
    private EntityManager entityManager;
    
    [Header("Grid Data")]
    public Vector3Int size = new Vector3Int(1, 5, 5); 
    public Vector3Int origin;  

    [Header("Properties")]
    public FlowField.CellType cellType = FlowField.CellType.Obstacle;
    public bool destructible = true;

    public void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        soldierEntity = entityManager.CreateEntity(typeof(ObstacleComponent));
        entityManager.SetComponentData(soldierEntity, new ObstacleComponent
        {
            
        });
    }

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