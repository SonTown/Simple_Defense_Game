using UnityEngine;

public class IsoCameraController : MonoBehaviour
{
    [Header("Target & Pivot")]
    public Transform target;          // 카메라가 바라볼 대상
    public Transform cameraPivot;     // 카메라가 붙어있는 pivot

    [Header("회전 설정")]
    public float rotationSpeed = 5f;  // 마우스 회전 감도
    private float currentAngle = 0f;

    [Header("줌 설정")]
    public float zoomSpeed = 5f;      // 줌 속도
    public float minZoom = 5f;        // 최소 줌 거리
    public float maxZoom = 20f;       // 최대 줌 거리
    private float currentZoom;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera가 필요합니다!");
            return;
        }

        // 카메라와 pivot 사이 초기 거리
        currentZoom = Vector3.Distance(cam.transform.position, cameraPivot.position);
    }

    void Update()
    {
        if (target == null || cameraPivot == null) return;

        // 1. 회전 (마우스 왼쪽 버튼 눌렀을 때만)
        if (Input.GetMouseButton(0)) // 0 = 왼쪽 버튼
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (Mathf.Abs(mouseX) > 0.01f)
            {
                currentAngle += mouseX * rotationSpeed;
            }
        }

        // pivot을 타겟 위치로 고정
        cameraPivot.position = target.position;
        cameraPivot.rotation = Quaternion.Euler(30f, currentAngle, 0f);

        // 2. 줌 처리 (마우스 휠)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }

        // 카메라 위치 갱신 (pivot 뒤쪽으로 일정 거리 유지)
        cam.transform.localPosition = new Vector3(0, 0, -currentZoom);
    }
}