using UnityEngine;
using Unity.Entities;

public class SpatialGridAuthoring : MonoBehaviour
{
    public int GridSizeX = 10;
    public int GridSizeY = 10;
    public int GridSizeZ = 10;
    public float CellSize = 1f;

    // Baker
    public class Baker : Baker<SpatialGridAuthoring>
    {
        public override void Bake(SpatialGridAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // NativeArray 초기화는 런타임에서 필요하면 따로 처리
            var data = new SpatialGridData
            {
                GridSizeX = authoring.GridSizeX,
                GridSizeY = authoring.GridSizeY,
                GridSizeZ = authoring.GridSizeZ,
                CellSize = authoring.CellSize// 런타임에 할당
            };

            AddComponent(entity, data);
        }
    }
}