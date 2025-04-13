using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Plank : MonoBehaviour
{
    // public bool movable; // 이 변수는 MainBall.cs에서 공의 상태에 따라 제어할 수 있습니다. (선택적)
                           // 여기서는 항상 움직일 수 있다고 가정하고 진행합니다.
                           // 만약 공이 발사되기 전에는 움직이지 않게 하려면 MainBall.cs에서 이 값을 조절해야 합니다.

    public Transform leftEnd = null;  // 왼쪽 이동 한계점 Transform
    public Transform rightEnd = null; // 오른쪽 이동 한계점 Transform

    [Tooltip("플랭크가 마우스를 따라가는 속도. 값이 클수록 빠르게 반응합니다.")]
    [Range(1f, 20f)] // Inspector에서 슬라이더로 조절 가능
    public float smoothSpeed = 10f; // 따라가는 속도 조절 변수

    public Camera mainCamera = null; // Public으로 변경하고 Inspector에서 할당
    private Plane plankPlane; // Raycast를 위한 평면

    void Start()
    {
        // mainCamera = Camera.main; // 더 이상 Camera.main 사용 안 함 (삭제)
        if (mainCamera == null)
        {
            // 오류 메시지를 Inspector 할당 확인으로 수정
            Debug.LogError("Main Camera가 Inspector에서 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }
        if (leftEnd == null || rightEnd == null)
        {
            Debug.LogError("Plank의 leftEnd 또는 rightEnd가 설정되지 않았습니다!", this);
            enabled = false;
            return;
        }
        if (leftEnd.position.x >= rightEnd.position.x)
        {
            Debug.LogWarning($"Plank 경고: leftEnd({leftEnd.position.x})의 x좌표가 rightEnd({rightEnd.position.x})보다 크거나 같습니다!", this);
        }
         Debug.Log($"Plank 초기화: LeftX={leftEnd.position.x:F3}, RightX={rightEnd.position.x:F3}");

        // 플랭크의 위치를 기준으로 Z축 방향의 평면 생성
        // Plane 생성자: 법선 벡터(normal), 평면 위의 한 점(point)
        plankPlane = new Plane(Vector3.forward, transform.position);
    }

    void Update()
    {
        // if (!movable) return; // 만약 movable 플래그로 제어하고 싶다면 이 줄의 주석을 해제하세요.

        if (leftEnd == null || rightEnd == null || mainCamera == null) return; // 이동 한계점이 없으면 실행 중단

        // --- 경계 시각화 (Scene 뷰에서 확인) ---
        // 플랭크 높이 기준으로 위아래로 빨간색(왼쪽), 녹색(오른쪽) 선 그리기
        Debug.DrawLine(new Vector3(leftEnd.position.x, transform.position.y - 0.5f, transform.position.z),
                       new Vector3(leftEnd.position.x, transform.position.y + 0.5f, transform.position.z), Color.red);
        Debug.DrawLine(new Vector3(rightEnd.position.x, transform.position.y - 0.5f, transform.position.z),
                       new Vector3(rightEnd.position.x, transform.position.y + 0.5f, transform.position.z), Color.green);
        // ------------------------------------

        Vector3 inputPosition = Vector3.zero;
        bool inputDetected = false;

        if (Input.GetMouseButton(0)) // 마우스 왼쪽 버튼 또는 터치
        {
            inputPosition = Input.mousePosition;
            inputDetected = true;
        }
        // // 터치 입력을 별도로 처리하려면 (멀티터치 등)
        // else if (Input.touchCount > 0)
        // {
        //     inputPosition = Input.GetTouch(0).position;
        //     inputDetected = true;
        // }

        if (inputDetected)
        {
            // 1. 마우스 위치로 Ray 생성
            Ray ray = mainCamera.ScreenPointToRay(inputPosition);

            // 2. Ray와 플랭크 평면의 교차점 계산
            float enterDistance;
            if (plankPlane.Raycast(ray, out enterDistance))
            {
                // 교차점 월드 좌표 얻기
                Vector3 worldPosition = ray.GetPoint(enterDistance);

                float targetXBeforeClamp = worldPosition.x;

                // 3. 목표 X 좌표 계산 및 제한
                float leftBoundaryX = leftEnd.position.x;
                float rightBoundaryX = rightEnd.position.x;
                float targetXAfterClamp = Mathf.Clamp(targetXBeforeClamp, leftBoundaryX, rightBoundaryX);

                // 4. 현재 위치에서 목표 위치까지 부드럽게 이동할 새 위치 계산
                // 목표 위치 벡터 생성 (Y, Z는 현재 플랭크 값 유지)
                Vector3 targetPosition = new Vector3(targetXAfterClamp, transform.position.y, transform.position.z);

                // Lerp를 사용하여 현재 위치에서 목표 위치로 부드럽게 이동
                // Time.deltaTime을 곱해주어 프레임 속도에 관계없이 일정한 속도로 움직이게 함
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

                // 5. 플랭크 위치를 계산된 부드러운 위치로 업데이트
                transform.position = smoothedPosition;

                 // --- 상세 추적 로그 (주석 처리)
                 Debug.Log($"MouseXY: {inputPosition.x:F0},{inputPosition.y:F0} | Viewport: {mainCamera.pixelRect} | WorldX: {worldPosition.x:F2} | TargetX_PreClamp: {targetXBeforeClamp:F2} | LeftB: {leftBoundaryX:F2} | RightB: {rightBoundaryX:F2} | TargetX_PostClamp: {targetXAfterClamp:F2} | CurrentX: {transform.position.x:F2}");
            }
            else
            {
                // Ray가 평면과 교차하지 않는 경우
                // Debug.LogWarning("Ray가 플랭크 평면과 교차하지 않습니다.");
            }
        }
    }

    // OnCollisionEnter (3D)는 2D 게임에서는 사용하지 않으므로 주석 처리 또는 삭제
    private void OnCollisionEnter(Collision other)
    {
        // ...
    }

    // bloodiness 관련 Update 로직도 필요 없다면 주석 처리 또는 삭제
    private float bloodiness = 0;
    private float timer = 0;
    private const float DELTA_BLOODINESS = 0.2F;
    private const float MAX_BLOODINESS = 1;
    private const float TIME_FOR_RECOVER = 3F;
    // ... Update 내 bloodiness 로직 ...
}
