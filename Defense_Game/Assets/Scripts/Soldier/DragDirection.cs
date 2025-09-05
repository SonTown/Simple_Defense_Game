using UnityEngine;

public class DragDirection : MonoBehaviour
{
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

        // 드래그 중
        if (isDragging && Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 currentPos = hit.point;
                direction = (currentPos - clickStartPos).normalized; // 방향
                float distance = Vector3.Distance(currentPos, clickStartPos); // 거리

                Debug.Log($"Drag direction: {direction}, distance: {distance}");
            }
        }

        // 마우스 떼면 드래그 종료
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            testSquad.MoveSquad(clickStartPos, direction);
        }
    }
}