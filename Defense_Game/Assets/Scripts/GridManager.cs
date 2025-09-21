using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public LayerMask groundLayer;
    public Camera mainCam;
    public GameObject obstaclePrefab;
    public GameObject previewPrefab;

    private Vector3Int size;
    private bool[,] grid;
    private GameObject previewObj;

    private List<Obstacle> obstacles = new List<Obstacle>();

    void Awake()
    {
        Instance = this;
        grid = new bool[FlowField.gridSizeX, FlowField.gridSizeY];

        previewObj = Instantiate(previewPrefab, Vector3.zero, Quaternion.identity);
        Obstacle obstacle = previewObj.GetComponent<Obstacle>();
        size = obstacle.size;
        previewObj.SetActive(false);
    }

    void Update()
    {
        UpdatePreview();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceObstacle();
        }
    }

    // === Preview ===
    void UpdatePreview()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Vector3 hitPoint = hit.point;
            Vector3Int gridPos = WorldToGrid(hitPoint);

            Vector3 worldPos = GridToWorld(gridPos, size);
            previewObj.transform.position = worldPos;
            previewObj.SetActive(true);
        }
        else
        {
            previewObj.SetActive(false);
        }
    }

    // === Placement ===
    void TryPlaceObstacle()
    {
        if (!previewObj.activeSelf) return;

        Vector3 worldPos = previewObj.transform.position;
        Vector3Int gridOrigin = WorldToGrid(worldPos);

        if (CanPlace(gridOrigin, size))
        {
            GameObject obj = Instantiate(obstaclePrefab, worldPos, Quaternion.identity);
            Obstacle obstacle = obj.GetComponent<Obstacle>();
            obstacle.size = size;
            obstacle.origin = gridOrigin;

            obstacles.Add(obstacle);
            Occupy(gridOrigin, size);
        }
    }

    // === Helpers ===
    public bool CanPlace(Vector3Int origin, Vector3Int size)
    {
        for (int x = -(size.x/2); x < (size.x+1)/2; x++)
        {
            for (int z = -(size.y/2); z < (size.y+1)/2; z++)
            {
                int gx = origin.x + x;
                int gz = origin.y + z;
                if (gx < 0 || gx >= FlowField.gridSizeX || gz < 0 || gz >= FlowField.gridSizeY) return false;
                if (grid[gx, gz]) return false;
            }
        }
        return true;
    }

    public void Occupy(Vector3Int origin, Vector3Int size)
    {
        for (int x = -(size.x/2); x < (size.x+1)/2; x++)
        {
            for (int z = -(size.y/2); z < (size.y+1)/2; z++)
            {
                grid[origin.x + x, origin.y + z] = true;
            }
        }
    }

    public void Release(Vector3Int origin, Vector3Int size)
    {
        for (int x = -(size.x/2); x < (size.x+1)/2; x++)
        {
            for (int z = -(size.y/2); z < (size.y+1)/2; z++)
            {
                grid[origin.x + x, origin.y + z] = false;
            }
        }
    }

    public Vector3Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / FlowField.cellSize);
        int y = Mathf.FloorToInt(worldPos.z / FlowField.cellSize);
        return new Vector3Int(x, y, 0);
    }

    public Vector3 GridToWorld(Vector3Int gridPos, Vector3Int size)
    {
        float worldX = gridPos.x * FlowField.cellSize+FlowField.cellSize / 2f;
        float worldZ = gridPos.y * FlowField.cellSize+FlowField.cellSize / 2f;
        return new Vector3(worldX, 0, worldZ);
    }

    // === Save & Load ===
    public void SaveObstacles()
    {
        string path = Path.Combine(Application.persistentDataPath, "obstacles.json");

        List<ObstacleData> dataList = new List<ObstacleData>();
        foreach (var obs in obstacles)
        {
            dataList.Add(obs.ToData());
        }

        ObstacleDataList wrapper = new ObstacleDataList(dataList.ToArray());
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(path, json);
        Debug.Log("Obstacles saved: " + path);
    }
}
