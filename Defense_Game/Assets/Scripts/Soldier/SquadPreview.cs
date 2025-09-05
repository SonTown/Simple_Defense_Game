using System;
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class SquadPreview : MonoBehaviour
{
    [Header("Settings")]
    public GhostSoldier ghostPrefab;
    public float spacing = 2f;
    public int totalSoldiers = 10;
    public Squad squad;
    private ObjectPool<GhostSoldier> pool;
    private List<GhostSoldier> activeGhosts = new List<GhostSoldier>();
    private int rows;
    private Vector3 squadCenter;
    void Awake() {
        pool = new ObjectPool<GhostSoldier>(
            createFunc: () => Instantiate(ghostPrefab, transform),
            actionOnGet: (ghost) => ghost.gameObject.SetActive(true),
            actionOnRelease: (ghost) => ghost.gameObject.SetActive(false),
            actionOnDestroy: (ghost) => Destroy(ghost.gameObject),
            collectionCheck: false, defaultCapacity: 50, maxSize: 200
        );
    }

    public void ShowPreview(Vector3 start, Vector3 end) {
        ClearPreview();

        Vector3 dir = (end - start);
        rows = Mathf.Abs(Mathf.RoundToInt((Mathf.Max(Vector3.Distance(start, end),3)-3)/2))+1;
        squadCenter = squad.CorrectToNavMesh(start);
        Vector3[] formationPositions = squad.CalculateFormationPositions(squadCenter, dir.normalized, rows, spacing);
        squad.ApplyTerrainCorrection(ref formationPositions);
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        foreach (Vector3 formation in formationPositions)
        {
            GhostSoldier ghost = pool.Get();
            ghost.Setup(formation, rot);
            activeGhosts.Add(ghost);
        }
    }

    public void ClearPreview() {
        foreach (var g in activeGhosts) pool.Release(g);
        activeGhosts.Clear();
    }
}
