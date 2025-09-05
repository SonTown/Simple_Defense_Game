using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Squad : MonoBehaviour
{
    [Header("Formation Settings")]
    public float spacing = 2f;
    public int rows = 2; // 기본 행 수

    [Header("Soldiers")]
    public List<Soldier> members = new List<Soldier>();
    public  List<NavMeshAgent> squadAgents = new List<NavMeshAgent>();

    private Vector3 squadCenter;
    private Vector3 forwardDir;

    // 객체 추가
    public void AddMember(Soldier soldier)
    {
        if (!members.Contains(soldier))
        {
            members.Add(soldier);
            NavMeshAgent agent = soldier.GetComponent<NavMeshAgent>();
            if (agent != null) squadAgents.Add(agent);
        }
    }

    // 객체 제거
    public void RemoveMember(Soldier soldier)
    {
        if (members.Contains(soldier))
        {
            int idx = members.IndexOf(soldier);
            members.Remove(soldier);
            if (idx < squadAgents.Count) squadAgents.RemoveAt(idx);
        }
    }

    public void MoveSquad(Vector3 targetPoint, Vector3 dragDirection)
    {
        
        rows = Mathf.Abs(Mathf.RoundToInt((Mathf.Max(dragDirection.magnitude,3)-3)/2))+1;
        squadCenter = CorrectToNavMesh(targetPoint);
        forwardDir = dragDirection.normalized;

        Vector3[] formationPositions = CalculateFormationPositions(squadCenter, forwardDir, rows, spacing);
        ApplyTerrainCorrection(ref formationPositions);
        SendAgentsToDestinations(formationPositions);
    }

    // 행 기준으로 중앙 기준 직사각형 배치 계산
    public Vector3[] CalculateFormationPositions(Vector3 center, Vector3 forward, int rows, float spacing)
    {
        int n = members.Count;
        if (n == 0) return new Vector3[0];

        int cols = Mathf.CeilToInt((float)n / rows);
        Vector3[] positions = new Vector3[n];

        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float offsetX = (cols - 1) * spacing * 0.5f;
        float offsetZ = (rows - 1) * spacing * 0.5f;

        for (int i = 0; i < n; i++)
        {
            int rowIndex = i % rows;   // 행 우선 채우기
            int colIndex = i / rows;   // 열

            Vector3 pos = center + right * (colIndex * spacing - offsetX) + forward * (rowIndex * spacing - offsetZ);
            positions[i] = pos;
        }

        return positions;
    }

    // NavMesh 기반 위치 보정
    public void ApplyTerrainCorrection(ref Vector3[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = CorrectToNavMesh(positions[i]);
        }
    }

    // 병사에게 이동 지시
    private void SendAgentsToDestinations(Vector3[] positions)
    {
        int n = Mathf.Min(positions.Length, squadAgents.Count);
        for (int i = 0; i < n; i++)
        {
            NavMeshAgent agent = squadAgents[i];
            agent.SetDestination(positions[i]);
            agent.transform.rotation = Quaternion.LookRotation(forwardDir);
        }
    }

    public Vector3 CorrectToNavMesh(Vector3 position, float maxDistance = 5f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return position;
    }
}
