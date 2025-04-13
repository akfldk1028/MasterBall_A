using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 열거형 정의
public enum SpawnableObjectType
{
    Brick,
    BonusBall,
    Star
}

public class ObjectPlacement : MonoBehaviour
{
    [Header("경계 참조")]
    [SerializeField] private Transform leftBorder;
    [SerializeField] private Transform rightBorder;
    [SerializeField] private Transform topBorder;
    
    [Header("오브젝트 프리팹")] // Resources.Load 대신 SerializeField 사용
    [SerializeField] private GameObject brickPrefab;
    [SerializeField] private GameObject bonusBallPrefab;
    [SerializeField] private GameObject starPrefab;
    
    [Header("레이아웃 설정")]
    [SerializeField] private int maxBricksPerRow = 7;
    [SerializeField] private float brickSpacing = 0.1f;
    [SerializeField] private float topOffset = -0.5f; // 음수값: topBorder 아래로
    [SerializeField] private float moveDownDistance = 0.85f;
    
    private float initialY;

    [Header("애니메이션 설정")]
    [SerializeField] private float movingDownStep = 0.04f; // 원래 MoveDownObjects 값
    
    // 현재 활성 오브젝트 목록
    private List<GameObject> activeObjects = new List<GameObject>();
    
    
    private void Awake()
    {
        // 프리팹 참조 확인 (Inspector에서 할당되지 않았을 경우 경고)
        if (brickPrefab == null) Debug.LogError("Brick Prefab이 할당되지 않았습니다!");
        if (bonusBallPrefab == null) Debug.LogError("Bonus Ball Prefab이 할당되지 않았습니다!");
        if (starPrefab == null) Debug.LogError("Star Prefab이 할당되지 않았습니다!");
    }
    
    // 열거형 타입에 맞는 프리팹 반환
    private GameObject GetPrefabForType(SpawnableObjectType type)
    {
        switch (type)
        {
            case SpawnableObjectType.Brick:     return brickPrefab;
            case SpawnableObjectType.BonusBall: return bonusBallPrefab;
            case SpawnableObjectType.Star:      return starPrefab;
            default:
                Debug.LogError($"알 수 없는 오브젝트 타입: {type}");
                return brickPrefab; // 기본값으로 Brick 반환
        }
    }
    
    // 오브젝트 배치 메서드
    public void PlaceNewObjectsOnTheScene()
    {
        // 이전 오브젝트 모두 한 단계 아래로 이동
        MoveDownAllObjects();
        
        // topBorder 위치 기준으로 초기 Y 위치 설정
        initialY = topBorder.position.y + (-2.5f); // topOffset이 음수이므로 결과적으로 빼기가 됨
        
        // 사용 가능한 너비 계산
        float availableWidth = rightBorder.position.x - leftBorder.position.x;
        
        // 벽돌 수 결정
        int numberOfBricks = (CommonVars.level < 10) ? Random.Range(1, 3) : Random.Range(2, 6);
        numberOfBricks = Mathf.Min(numberOfBricks, maxBricksPerRow);
        
        // 벽돌 너비 및 위치 계산
        float totalWidth = rightBorder.position.x - leftBorder.position.x;
        float brickWidth = (totalWidth - (brickSpacing * (maxBricksPerRow - 1))) / maxBricksPerRow;

        // 사용할 위치 선택
        List<int> positions = new List<int>();
        for (int i = 0; i < maxBricksPerRow; i++) positions.Add(i);
        
        // 랜덤하게 섞기
        for (int i = 0; i < positions.Count; i++)
        {
            int temp = positions[i];
            int randomIndex = Random.Range(i, positions.Count);
            positions[i] = positions[randomIndex];
            positions[randomIndex] = temp;
        }
        
        // 선택된 위치에 오브젝트 생성
        for (int i = 0; i < numberOfBricks; i++)
        {
            int posIndex = positions[i];
            
            // 왼쪽 모서리부터 시작해서 각 벽돌 위치 계산
            // 1. 첫 번째 벽돌 왼쪽 모서리 = leftBorder 위치
            // 2. 이후 벽돌들은 (너비 + 간격)만큼 오른쪽으로 이동
            float leftEdgeX = leftBorder.position.x + (posIndex * (brickWidth + brickSpacing));
            
            // 중심점은 왼쪽 모서리 + 너비의 절반
            float centerX = leftEdgeX + (brickWidth / 2);
            
            Vector3 spawnPosition = new Vector3(centerX, initialY, 0);
            
            // --- 오브젝트 타입 결정 및 생성 방식 변경 ---
            SpawnableObjectType objectType = SpawnableObjectType.Brick; // 기본값
            int randomType = Random.Range(0, 20); // 0~19 범위

            if (randomType == 0) // 1/20 확률 (5%)
                objectType = SpawnableObjectType.BonusBall;
            else if (randomType == 1) // 1/20 확률 (5%)
                objectType = SpawnableObjectType.Star;

            // 열거형으로 프리팹 가져오기
            GameObject prefabToSpawn = GetPrefabForType(objectType);

            if (prefabToSpawn != null)
            {
                // 오브젝트 생성 및 설정
                GameObject newObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                
                // 정확한 너비로 설정 (중요!)
                newObject.transform.localScale = new Vector3(brickWidth, brickWidth * 0.8f, 1);
                
                // Rigidbody2D 컴포넌트 설정
                Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
                if (rb == null) rb = newObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.isKinematic = true;
                
                // ObjectData 컴포넌트 추가
                ObjectData data = newObject.AddComponent<ObjectData>();
                data.rb = rb;
                
                // 첫 행이면 아래로 천천히 이동
                StartCoroutine(MoveFirstRowDown(newObject, initialY));
                
                // 리스트에 추가
                activeObjects.Add(newObject);
            }
            // --- 변경 끝 ---
        }
    }
    
    // 첫 행을 천천히 아래로 이동시키는 코루틴
    private IEnumerator MoveFirstRowDown(GameObject obj, float startY)
    {
        float targetY = initialY; // 인스펙터에서 설정한 최종 위치
        if (obj == null) yield break;
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;

        // Debug.Log($"Moving from {obj.transform.position.y} to {targetY}");

        while (obj != null && obj.transform.position.y > targetY)
        {
            Vector3 pos = obj.transform.position;
            pos.y -= movingDownStep * Time.fixedDeltaTime * 50; // 속도 조절 (기존 값과 비슷하게)

            if (pos.y <= targetY)
                pos.y = targetY;

            rb.MovePosition(pos);
            yield return new WaitForFixedUpdate(); // FixedUpdate 기준으로 대기
        }
    }
    
    // 모든 활성 오브젝트를 한 단계 아래로 이동
    private void MoveDownAllObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            
            if (obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }
            
            // 현재 위치에서 아래로 이동
            float newY = obj.transform.position.y - moveDownDistance;
            
            // 화면 밖으로 나갔는지 확인
            if (newY < -2.3f) // 기존 하드코딩 값
            {
                // 벽돌이 바닥에 닿으면 게임 오버
                if (obj.name.Contains("brick"))
                {
                    // 게임 오버 로직
                    Debug.Log("게임 오버: 벽돌이 바닥에 닿음");
                }
                
                Destroy(obj);
                activeObjects.RemoveAt(i);
            }
            else
            {
                // 부드러운 이동 시작
                StartCoroutine(MoveDown(obj, newY));
            }
        }
    }
    
    // 오브젝트를 천천히 아래로 이동시키는 코루틴
    private IEnumerator MoveDown(GameObject obj, float targetY)
    {
        if (obj == null) yield break;
        
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;
        
        float startY = obj.transform.position.y;
        
        // 천천히 아래로 이동
        while (obj != null && obj.transform.position.y > targetY)
        {
            Vector3 pos = obj.transform.position;
            pos.y -= movingDownStep;
            
            if (pos.y <= targetY)
                pos.y = targetY;
                
            rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }
    }
    
    // 경계 Gizmo 표시 (에디터에서만)
    private void OnDrawGizmos()
    {
        if (leftBorder && rightBorder && topBorder)
        {
            Gizmos.color = Color.yellow;
            float initialY = topBorder.position.y + topOffset;
            float leftBoundary = leftBorder.position.x + (leftBorder.localScale.x / 2);
            float rightBoundary = rightBorder.position.x - (rightBorder.localScale.x / 2);
            Gizmos.DrawLine(new Vector3(leftBoundary, initialY, 0), new Vector3(rightBoundary, initialY, 0)); // 초기 스폰 라인

            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(leftBoundary, initialY, 0), new Vector3(rightBoundary, initialY, 0)); // 첫 행 최종 라인


            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(leftBoundary, -2.3f, 0), new Vector3(rightBoundary, -2.3f, 0)); // 게임 오버 라인
        }
    }

    // 오브젝트 데이터 저장용 컴포넌트
    private class ObjectData : MonoBehaviour
    {
        public Rigidbody2D rb;
        public bool isFirstRow = true;
    }
}
