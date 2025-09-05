using UnityEngine;

public class DragDirection : MonoBehaviour
{
    public SquadPreview squadPreview;
    private Vector3 clickStartPos;
    private bool isDragging = false;
    public Squad testSquad;
    private Vector3 direction;
    void Update()
    {
        // 마우스 클릭 시작
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                clickStartPos = hit.point; // 클릭한 지형 위치
                isDragging = true;
                Debug.Log("Clicked at: " + clickStartPos);
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

            if (Physics.Raycast(ray, out hit))
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
}