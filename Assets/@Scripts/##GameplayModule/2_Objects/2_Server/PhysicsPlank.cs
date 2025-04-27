using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using UnityEngine;

public class PhysicsPlank : PhysicsObject
{
    [Header("Movement Limits")]
    public Transform leftEnd;
    public Transform rightEnd;

    [Header("Movement Settings")]
    [Tooltip("플랭크가 마우스를 따라가는 속도")]
    [Range(1f, 20f)]
    public float smoothSpeed = 20f;
    
    [Header("Control")]
    public bool CanMove = true;

    [Header("References")]
    public Camera mainCamera;
    
    private Plane plankPlane;
    
    void Start()
    {
        // 필수 컴포넌트 검증
        if (mainCamera == null || leftEnd == null || rightEnd == null)
        {
            string missingComponent = mainCamera == null ? "Main Camera" : 
                                     (leftEnd == null ? "leftEnd" : "rightEnd");
            Debug.LogError($"{missingComponent}가 Inspector에서 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }
        
        // 경계 위치 검증
        if (leftEnd.position.x >= rightEnd.position.x)
        {
            Debug.LogWarning($"Plank 경고: leftEnd({leftEnd.position.x})의 x좌표가 rightEnd({rightEnd.position.x})보다 크거나 같습니다!", this);
        }

        // 이동 평면 초기화
        plankPlane = new Plane(Vector3.forward, transform.position);
    }

    void Update()
    {
        if (!IsComponentsValid()) return;
        
        // Check if movement is allowed
        if (!CanMove) return;

        // 입력 감지
        if (!Input.GetMouseButton(0)) return;
        
        // 입력 위치 변환
        Vector3 targetPosition = GetTargetPositionFromInput(Input.mousePosition);
        if (targetPosition == Vector3.zero) return;
        
        // 플랭크 이동
        MovePlank(targetPosition);
    }
    
    /// <summary>
    /// 필수 컴포넌트가 유효한지 확인
    /// </summary>
    private bool IsComponentsValid()
    {
        return mainCamera != null && leftEnd != null && rightEnd != null;
    }
    
    /// <summary>
    /// 입력 위치로부터 대상 위치 계산
    /// </summary>
    private Vector3 GetTargetPositionFromInput(Vector3 inputPosition)
    {
        // 마우스 위치로 Ray 생성
        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        
        // Ray와 플랭크 평면 교차점 계산
        if (!plankPlane.Raycast(ray, out float enterDistance))
            return Vector3.zero;
            
        // 세계 좌표 계산 및 경계 제한
        Vector3 worldPosition = ray.GetPoint(enterDistance);
        float targetX = Mathf.Clamp(worldPosition.x, leftEnd.position.x, rightEnd.position.x);
        
        return new Vector3(targetX, transform.position.y, transform.position.z);
    }
    
    /// <summary>
    /// 플랭크를 목표 위치로 이동
    /// </summary>
    private void MovePlank(Vector3 targetPosition)
    {
        // 현재 위치에서 목표 위치로 부드럽게 이동
        Vector3 newPosition = Vector3.MoveTowards(
            transform.position, 
            targetPosition, 
            smoothSpeed * Time.deltaTime
        );
        
        // 물리 또는 트랜스폼으로 이동
        if (rb != null && rb.isKinematic)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
    }

  
}