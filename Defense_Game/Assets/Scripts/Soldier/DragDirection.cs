using UnityEngine;

public class DragDirection : MonoBehaviour
{
    public SquadPreview squadPreview;
    private Vector3 clickStartPos;
    private bool isDragging = false;
    public SquadManager squadManager;
    public Squad testSquad;
    private Vector3 direction;
    public LayerMask layerMask;
    void Update()
    {
        // 마우스 클릭 시작
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit,Mathf.Infinity,layerMask))
                {
                    clickStartPos = hit.point; // 클릭한 지형 위치
                    isDragging = true;
                    Debug.Log("Clicked at: " + clickStartPos);
                }
            }
        }
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString())) // "1" ~ "9"
            {
                TrySelectSquad(i);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            isDragging = false;
            squadPreview.ClearPreview();
        }

        // 드래그 중
        if (isDragging && Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit,Mathf.Infinity,layerMask))
            {
                Vector3 currentPos = hit.point;
                direction = (currentPos - clickStartPos); // 방향
                squadPreview.ShowPreview(clickStartPos, hit.point);
            }
        }

        // 마우스 떼면 드래그 종료
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            testSquad.MoveSquad(clickStartPos, direction);
            squadPreview.ClearPreview();
        }
    }
    void TrySelectSquad(int index)
    {
        Squad squad = squadManager.SelectSquad(index);

        if (squad != null)
        {
            testSquad = squad;
            squadPreview.SetSquad(squad);
            Debug.Log($"스쿼드 {index} 선택됨!");
        }
        else
        {
            Debug.Log($"스쿼드 {index} 없음 (무시)");
        }
    }
}