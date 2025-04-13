using System.Collections;
using System.Collections.Generic;
using System.Linq; // For ToList()
using UnityEngine;
// using Unity.Assets.Scripts.Objects; // 네임스페이스 참조는 유지 (ObjectSpawner 사용 안해도 Brick 등 참조 위함)

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
    
    [Header("오브젝트 프리팹")] // ObjectSpawner 없으므로 직접 참조 유지
    [SerializeField] private GameObject brickPrefab;
    [SerializeField] private GameObject bonusBallPrefab;
    [SerializeField] private GameObject starPrefab;
    
    [Header("레이아웃 설정")]
    [SerializeField] private int maxBricksPerRow = 7;
    [SerializeField] private float brickSpacing = 0.1f;
    [SerializeField] private float topOffset = -2.5f; // 첫 행이 최종적으로 위치할 Y 오프셋
    [SerializeField] private float moveDownDistance = 0.85f; // 한 칸 내려갈 거리
    [SerializeField] private int numberOfRowsToSpawn = 3;   // 새로 추가: 한 번에 생성할 행 수
    [SerializeField] private float rowSpacing = 0.8f;       // 새로 추가: 행 간 세로 간격
    [SerializeField] private float objectScaleMultiplier = 0.7f; // 새로 추가: 오브젝트 크기 배율
    [SerializeField] private int totalObjectsToSpawn = 50;      // 다시 추가: 생성할 총 오브젝트 수 (랜덤 선택용)
    
    // 생성 시 Y 오프셋 값을 0으로 변경 (topBorder 위치에서 생성)
    private float initialSpawnYOffset = 0f; // 생성 시 Y 오프셋 (기존 -2.5f 값에서 변경)

    [Header("애니메이션 설정")]
    [SerializeField] private float movingDownStep = 0.04f;
    
    // List<GameObject> 대신 Dictionary 사용
    private Dictionary<GameObject, bool> activeObjectData = new Dictionary<GameObject, bool>();
    
    
    private void Awake()
    {
        // Spawner 참조 확인 제거
        // 프리팹 null 체크는 그대로 유지
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
    
    // 내부 구조체 또는 클래스로 위치 정보 저장
    private struct PotentialSpawnInfo
    {
        public Vector3 SpawnPosition; 
        public float TargetY;
    }

    // 오브젝트 배치 메서드
    public void PlaceNewObjectsOnTheScene()
    {
        Debug.Log("[ObjectPlacement] Placing new objects (random grid, adjacent).");
        MoveDownAllObjects();
        
        // --- 1. 모든 잠재적 스폰 위치 정보 계산 (붙이기 로직 유지) --- 
        List<PotentialSpawnInfo> potentialSpawns = new List<PotentialSpawnInfo>();

        float baseSpawnY = topBorder.position.y + initialSpawnYOffset;
        float baseTargetY = topBorder.position.y + topOffset;
        float totalWidth = rightBorder.position.x - leftBorder.position.x;
        
        float baseBrickWidth = totalWidth / maxBricksPerRow; 
        Vector3 objectScale = new Vector3(baseBrickWidth, baseBrickWidth * 0.8f, 1) * objectScaleMultiplier;
        float actualObjectWidth = objectScale.x;
        float actualObjectHeight = objectScale.y;

        // --- 실제 배치 가능한 열(Column) 수 계산 --- 
        int calculatedFitCount = 0;
        if (actualObjectWidth > 0) // 0으로 나누기 방지
        {
            calculatedFitCount = Mathf.FloorToInt(totalWidth / actualObjectWidth);
        }
        else
        {
             Debug.LogWarning("[ObjectPlacement] Calculated actualObjectWidth is zero or negative. Cannot calculate fit count.");
        }
        // Inspector 설정(maxBricksPerRow)과 계산된 값 중 작은 값을 최종 사용 -> 계산된 값만 사용하도록 변경
        int finalColumnCount = calculatedFitCount; // 계산된 값 직접 사용
        Debug.Log($"[ObjectPlacement] TotalWidth: {totalWidth:F2}, ActualWidth: {actualObjectWidth:F2}, CalculatedFit: {calculatedFitCount}, (MaxPerRow Ignored), FinalCols: {finalColumnCount}");
        // --- 계산 끝 --- 

        for (int rowIndex = 0; rowIndex < numberOfRowsToSpawn; rowIndex++)
        {   
            float targetY = baseTargetY - (rowIndex * actualObjectHeight);
            // colIndex 루프 조건을 finalColumnCount 로 변경    
            for (int colIndex = 0; colIndex < finalColumnCount; colIndex++)
            {
                float leftEdgeX = leftBorder.position.x + (colIndex * actualObjectWidth);
                float centerX = leftEdgeX + (actualObjectWidth / 2);
                Vector3 spawnPosition = new Vector3(centerX, baseSpawnY, 0);
                
                potentialSpawns.Add(new PotentialSpawnInfo { SpawnPosition = spawnPosition, TargetY = targetY });
            }
        }
        Debug.Log($"[ObjectPlacement] Calculated {potentialSpawns.Count} potential spawn slots.");

        // --- 2. 스폰 위치 랜덤 셔플 (복원) --- 
        System.Random rng = new System.Random();
        int n = potentialSpawns.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            PotentialSpawnInfo value = potentialSpawns[k];
            potentialSpawns[k] = potentialSpawns[n];
            potentialSpawns[n] = value;
        }

        // --- 3. 선택된 위치에 오브젝트 생성 (복원) --- 
        int objectsToSpawnCount = Mathf.Min(totalObjectsToSpawn, potentialSpawns.Count); // 실제 생성 개수 제한
        Debug.Log($"[ObjectPlacement] Spawning {objectsToSpawnCount} objects randomly (adjacent).");

        for (int i = 0; i < objectsToSpawnCount; i++)
        {
            PotentialSpawnInfo spawnInfo = potentialSpawns[i];
            Vector3 spawnPosition = spawnInfo.SpawnPosition;
            float targetY = spawnInfo.TargetY;

            // 오브젝트 타입 랜덤 결정
            SpawnableObjectType objectType = SpawnableObjectType.Brick;
            int randomType = Random.Range(0, 20); // 확률 조정 필요 시 이 값 변경
            if (randomType == 0) objectType = SpawnableObjectType.BonusBall;
            else if (randomType == 1) objectType = SpawnableObjectType.Star;

            GameObject prefabToSpawn = GetPrefabForType(objectType);

            if (prefabToSpawn != null)
            {
                GameObject newObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                newObject.transform.localScale = objectScale;
                
                Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
                if (rb == null) rb = newObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.isKinematic = true;
                
                if (!activeObjectData.ContainsKey(newObject))
                {
                    activeObjectData.Add(newObject, true); // isFirstRow = true
                }
                else
                {
                    Debug.LogWarning($"[ObjectPlacement] Duplicate GameObject key: {newObject.name}");
                }

                StartCoroutine(MoveObjectToTargetY(newObject, targetY));
            }
        }
        // --- 생성 로직 끝 --- 
    }
    
    // 코루틴 이름 변경 및 targetY 파라미터 추가
    private IEnumerator MoveObjectToTargetY(GameObject obj, float targetY)
    {
        if (obj == null) yield break;
        float startY = obj.transform.position.y;
        Debug.Log($"[ObjectPlacement] Starting MoveObjectToTargetY for {obj.name} from {startY} to {targetY}");

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;

        // 목표 지점에 도달할 때까지 이동
        while (obj != null && Mathf.Abs(obj.transform.position.y - targetY) > 0.01f) // 목표 Y에 근접할 때까지
        {
            Vector3 pos = obj.transform.position;
            // 목표 지점까지 부드럽게 이동 (Lerp 방식 또는 현재 방식 유지 - 현재 방식 사용)
            float step = movingDownStep * Time.fixedDeltaTime * 50;
            pos.y = Mathf.MoveTowards(pos.y, targetY, step);

            rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }

        // 최종 위치 설정 및 상태 업데이트
        if (obj != null)
        {
            Debug.Log($"[ObjectPlacement] Finished MoveObjectToTargetY for {obj.name} at {targetY}");
            // 정확한 최종 위치로 설정
            Vector3 finalPos = obj.transform.position; 
            finalPos.y = targetY;
            rb.MovePosition(finalPos);

            if (activeObjectData.ContainsKey(obj))
            {
                activeObjectData[obj] = false; // isFirstRow = false
            }
        }
        else
        {
            Debug.LogWarning($"[ObjectPlacement] Object destroyed during MoveObjectToTargetY.");
        }
    }
    
    // 모든 활성 오브젝트를 한 단계 아래로 이동
    private void MoveDownAllObjects()
    {
        Debug.Log($"[ObjectPlacement] Moving down all {activeObjectData.Count} active objects.");

        List<GameObject> keysToRemove = new List<GameObject>();
        // Dictionary 복사본 생성 후 순회 (순회 중 삭제 대비)
        var currentActiveObjects = activeObjectData.ToList(); 

        foreach (KeyValuePair<GameObject, bool> pair in currentActiveObjects)
        {
            GameObject obj = pair.Key;
            bool isFirstRow = pair.Value;

            if (obj == null) // 오브젝트가 파괴된 경우 (예: 다른 스크립트에서)
            {
                Debug.LogWarning($"[ObjectPlacement] Found destroyed object key in dictionary. Marking for removal.");
                keysToRemove.Add(obj); // Dictionary에서 제거하도록 표시
                continue;
            }

            // --- ObjectData 대신 Dictionary 값 사용 --- 
            if (isFirstRow)
            {
                continue; // 첫 행 이동 중이면 건너뜀
            }
            // --- 변경 끝 ---

            float newY = obj.transform.position.y - moveDownDistance;
            const float bottomBoundary = -2.3f;

            if (newY < bottomBoundary)
            {
                Debug.Log($"[ObjectPlacement] Object {obj.name} reached bottom boundary ({newY}). Destroying and marking for removal.");
                Destroy(obj); // 오브젝트 파괴
                keysToRemove.Add(obj); // Dictionary에서 제거하도록 표시
            }
            else
            {
                StartCoroutine(MoveDown(obj, newY));
            }
        }

        // 순회가 끝난 후 표시된 키들을 Dictionary에서 제거
        foreach (GameObject key in keysToRemove)
        {
            activeObjectData.Remove(key);
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
            float leftBoundary = leftBorder.position.x;
            float rightBoundary = rightBorder.position.x;

            // --- Gizmo 표시를 위한 크기 계산 (PlaceNewObjectsOnTheScene과 동일 로직) ---
            float totalWidth = rightBoundary - leftBoundary;
            int colsToDraw = maxBricksPerRow; // Gizmo는 maxBricksPerRow 기준으로 그림 (실제 배치는 다를 수 있음)
            float baseBrickWidth = (colsToDraw > 0) ? totalWidth / colsToDraw : totalWidth; // 0 나누기 방지
            Vector3 gizmoObjectScale = new Vector3(baseBrickWidth, baseBrickWidth * 0.8f, 1) * objectScaleMultiplier;
            float actualObjectHeight = gizmoObjectScale.y; // 실제 스케일 y값 사용
            // --- 계산 끝 ---

            // 초기 스폰 영역 상단 표시 (참고용) -> 실제 스폰 Y 위치로 변경
            Gizmos.color = Color.cyan;
            float baseSpawnY = topBorder.position.y + initialSpawnYOffset;
            Gizmos.DrawLine(new Vector3(leftBoundary, baseSpawnY, 0), 
                            new Vector3(rightBoundary, baseSpawnY, 0)); // 수정된 코드: 실제 스폰 위치에 표시

            // 각 행의 최종 목표 위치 표시
            Gizmos.color = Color.green;
            float baseTargetY = topBorder.position.y + topOffset;
            for (int i = 0; i < numberOfRowsToSpawn; i++)
            {
                // targetY 계산 시 rowSpacing 대신 actualObjectHeight 사용
                float targetY = baseTargetY - (i * actualObjectHeight);
                Gizmos.DrawLine(new Vector3(leftBoundary, targetY, 0), new Vector3(rightBoundary, targetY, 0));
            }

            // 게임 오버 라인
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(leftBoundary, -2.3f, 0), new Vector3(rightBoundary, -2.3f, 0));
        }
    }
}
