using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 추가

public class IsometricGridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject cubePrefab;
    public GameObject wallPrefab; // New wall prefab
    public int gridSizeX = 20;
    public int gridSizeY = 20;
    public float aspectRatio = 1.5f; // Adjust this value to make the grid appear square

    public float cubeSize = 1.0f;
    public float spacing = 0.05f;
    public float gridHeight = 0.2f;
    public float wallHeight = 1.0f; // Height for walls
    
    [Header("Player Settings")] // 플레이어 수 설정 추가
    public int playerCount = 4; // 기본값 4명

    [Header("Material Settings")] // 머티리얼 설정을 위한 헤더 추가
    public Material borderMaterial; // BorderMaterial 참조 변수 추가

    [Header("Turret Settings")] // 터렛 설정 추가
    public GameObject standardTurretPrefab; // Standard Turret 프리팹 참조
    public float turretHeightOffset = 0.5f; // 터렛 배치 높이 오프셋

    // 생성된 캐논 리스트
    private List<Cannon> _cannons = new List<Cannon>();
    
    // 생성된 캐논 리스트에 접근할 수 있는 프로퍼티
    public List<Cannon> Cannons => _cannons;

    // 블록 소유권 관리를 위한 변수 추가
    private Dictionary<GameObject, int> _blockOwners = new Dictionary<GameObject, int>();
    private Dictionary<int, Color> _playerColors = new Dictionary<int, Color>();

    public static IsometricGridGenerator Instance { get; private set; }

    void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("IsometricGridGenerator 인스턴스가 이미 존재합니다. 새로 생성된 인스턴스를 파괴합니다.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (borderMaterial == null)
        {
            Debug.LogError("Border Material이 Inspector에서 할당되지 않았습니다!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // borderMaterial이 할당되었는지 확인
        if (borderMaterial == null)
        {
            Debug.LogError("Border Material이 Inspector에서 할당되지 않았습니다!");
            enabled = false; // 오류 발생 시 스크립트 비활성화
            return;
        }
        CreateGrid();
    }
    
    public void CreateGrid()
    {
        // Clear existing objects and reset cannons list
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _cannons.Clear(); // 캐논 리스트 초기화
        _blockOwners.Clear(); // 블록 소유권 정보 초기화
        
        // *** 플레이어 색상 정의를 먼저 호출 ***
        DefinePlayerColors();
        
        float step = cubeSize + spacing;
        float startX = -(gridSizeX * step) / 2f;
        
        // Adjust the Z dimension by the aspect ratio to make it appear square
        int adjustedGridSizeY = Mathf.RoundToInt(gridSizeY * aspectRatio);
        float startZ = -(adjustedGridSizeY * step) / 2f;
        
        // Create grid tiles
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < adjustedGridSizeY; y++)
            {
                float posX = startX + x * step + cubeSize/2;
                float posZ = startZ + y * step + cubeSize/2;
                
                Vector3 position = new Vector3(posX, 0, posZ);
                
                GameObject cube;
                if (cubePrefab != null)
                {
                    cube = Instantiate(cubePrefab, position, Quaternion.identity);
                }
                else
                {
                    cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = position;
                }
                
                // 태그 설정
                cube.tag = "GridBlock";
                
                cube.transform.localScale = new Vector3(cubeSize, gridHeight, cubeSize);
                cube.transform.parent = this.transform;
                
                // 콜라이더 크기 조정 - 충돌 감지를 위해 위쪽으로 확장
                BoxCollider collider = cube.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    // 콜라이더의 중심을 위로 이동하고 높이를 증가
                    collider.center = new Vector3(0, 5.0f, 0);
                    collider.size = new Vector3(1, 10.0f, 1);
                    
                    // 트리거로 설정하여 물리적 충돌 없이 이벤트만 발생하도록 함
                    collider.isTrigger = true;
                }
                
                Renderer renderer = cube.GetComponent<Renderer>();
                
                // 초기 블록 소유권 설정
                int ownerID = GetInitialOwnerID(x, y, gridSizeX, adjustedGridSizeY);
                
                // 블록 소유권 딕셔너리에 추가
                _blockOwners[cube] = ownerID;
                
                // 디버그: 초기 소유권 할당 확인
                Debug.Log($"<color=#ADD8E6>큐브 생성: ({x},{y}), 계산된 소유자 ID = {ownerID}</color>");
                
                // 렌더러가 있는 경우 소유자 색상 적용
                if (renderer != null)
                {
                    // 소유자 색상 가져오기
                    if (_playerColors.TryGetValue(ownerID, out Color ownerColor))
                    {
                        // 소유자 색상 적용
                        renderer.material.color = ownerColor;
                        Debug.Log($"<color=cyan>큐브 초기화 완료: ({x},{y}), 소유자={ownerID}, 색상={ownerColor}</color>");
                    }
                    else
                    {
                        // 오류: 플레이어 색상이 정의되지 않음 (기본 흰색으로 처리)
                        renderer.material.color = Color.white;
                        Debug.LogError($"<color=red>오류: 플레이어 {ownerID}의 색상이 정의되지 않았습니다!</color>");
                    }
                }
            }
        }

        // --- 터렛 배치 로직 추가 ---
        if (standardTurretPrefab == null)
        {
            Debug.LogError("Standard Turret Prefab이 Inspector에서 할당되지 않았습니다!");
        }
        else
        {
            // 모서리 좌표 계산 (타일 중심 기준)
            float halfCubeSize = cubeSize / 2f;
            float turretYPos = gridHeight + turretHeightOffset; // 타일 높이 위에 배치

            // 각 모서리 중심 좌표
            Vector3 bottomLeftPos = new Vector3(startX + halfCubeSize, turretYPos, startZ + halfCubeSize);
            Vector3 topRightPos = new Vector3(startX + (gridSizeX - 1) * step + halfCubeSize, turretYPos, startZ + (adjustedGridSizeY - 1) * step + halfCubeSize);
            Vector3 topLeftPos = new Vector3(startX + halfCubeSize, turretYPos, startZ + (adjustedGridSizeY - 1) * step + halfCubeSize);
            Vector3 bottomRightPos = new Vector3(startX + (gridSizeX - 1) * step + halfCubeSize, turretYPos, startZ + halfCubeSize);

            // 플레이어 색상 정의 (각 플레이어의 고유 색상)
            DefinePlayerColors();

            if (playerCount == 2)
            {
                // *** 플레이어 0과 1의 터렛 위치 교체 ***
                // 이전: 좌상단(0), 우하단(1)
                // 변경: 우하단(0), 좌상단(1)
                InstantiateTurret(standardTurretPrefab, bottomRightPos, transform, 0); // 우하단 - 플레이어 0번
                InstantiateTurret(standardTurretPrefab, topLeftPos, transform, 1); // 좌상단 - 플레이어 1번
            }
            else // 4인 또는 그 외
            {
                // 네 모서리에 모두 배치 (4인용은 그대로 유지)
                InstantiateTurret(standardTurretPrefab, bottomLeftPos, transform, 0); // 좌하단 - 플레이어 0번
                InstantiateTurret(standardTurretPrefab, topRightPos, transform, 1); // 우상단 - 플레이어 1번
                InstantiateTurret(standardTurretPrefab, topLeftPos, transform, 2); // 좌상단 - 플레이어 2번
                InstantiateTurret(standardTurretPrefab, bottomRightPos, transform, 3); // 우하단 - 플레이어 3번
            }
        }
        
        // Add walls around the grid - update this to use adjustedGridSizeY
        CreateWalls(startX, startZ, step, adjustedGridSizeY);
    }
        
    void CreateWalls(float startX, float startZ, float step, int adjustedGridSizeY)
    {
        // Calculate the full grid dimensions
        float gridWidth = gridSizeX * step;
        float gridDepth = adjustedGridSizeY * step;
        
        // Create the four walls
        // Top wall (Z+)
        CreateWallRow(startX, startZ + gridDepth, gridWidth, true);
        
        // Bottom wall (Z-)
        CreateWallRow(startX, startZ - step, gridWidth, true);
        
        // Left wall (X-)
        CreateWallRow(startX - step, startZ, gridDepth, false);
        
        // Right wall (X+)
        CreateWallRow(startX + gridWidth, startZ, gridDepth, false);
        
        // Add corner pieces
        CreateCornerPiece(startX - step, startZ - step);
        CreateCornerPiece(startX + gridWidth, startZ - step);
        CreateCornerPiece(startX - step, startZ + gridDepth);
        CreateCornerPiece(startX + gridWidth, startZ + gridDepth);
    }
    void CreateCornerPiece(float x, float z)
    {
        Vector3 position = new Vector3(x + cubeSize/2, wallHeight/2, z + cubeSize/2);
        
        GameObject corner;
        if (wallPrefab != null)
        {
            corner = Instantiate(wallPrefab, position, Quaternion.identity);
        }
        else
        {
            corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corner.transform.position = position;
        }
        
        // Wall 태그 설정
        corner.tag = "Wall";
        
        corner.transform.localScale = new Vector3(cubeSize, wallHeight, cubeSize);
        corner.transform.parent = this.transform;
        
        // Set wall material
        Renderer renderer = corner.GetComponent<Renderer>();
        if (renderer != null && borderMaterial != null) // borderMaterial이 null이 아닌지 확인
        {
            // renderer.material.color = wallColor; // 이전 색상 설정 제거
            renderer.material = borderMaterial; // 머티리얼 할당
        }
    }
    void CreateWallRow(float startX, float startZ, float length, bool isHorizontal)
    {
        float step = cubeSize + spacing;
        int segments = Mathf.CeilToInt(length / step);
        
        for (int i = 0; i < segments; i++)
        {
            float posX = isHorizontal ? startX + i * step + cubeSize/2 : startX + cubeSize/2;
            float posZ = isHorizontal ? startZ + cubeSize/2 : startZ + i * step + cubeSize/2;
            
            Vector3 position = new Vector3(posX, wallHeight/2, posZ);
            
            GameObject wall;
            if (wallPrefab != null)
            {
                wall = Instantiate(wallPrefab, position, Quaternion.identity);
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = position;
            }
            
            // Wall 태그 설정
            wall.tag = "Wall";
            
            // Set wall dimensions
            if (isHorizontal)
            {
                wall.transform.localScale = new Vector3(cubeSize, wallHeight, cubeSize);
            }
            else
            {
                wall.transform.localScale = new Vector3(cubeSize, wallHeight, cubeSize);
            }
            
            wall.transform.parent = this.transform;
            
            // Set wall material
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null && borderMaterial != null) // borderMaterial이 null이 아닌지 확인
            {
                // renderer.material.color = wallColor; // 이전 색상 설정 제거
                renderer.material = borderMaterial; // 머티리얼 할당
            }
        }
    }

    // Helper method to get color based on Brick.cs logic
    private Color GetColorFromBrickLogic(int number)
    {
        if (number <= 30)
        {
            return new Color(1, 1 - (number / 30f), 0); // Yellow to Red
        }
        else if (number <= 60)
        {
            return new Color(1, 0, (number - 30) / 30f); // Red to Purple
        }
        else
        {
            float redColorValue = 1 - ((number - 60) / 30f);
            return new Color(Mathf.Max(redColorValue, 0), 0, 1); // Purple to Blue
        }
    }

    Color GetCubeColor(int x, int y, int adjustedGridSizeY)
    {
        // 플레이어 수에 따라 로직 분기
        if (playerCount == 2)
        {
            // 2인용: 좌하단 vs 우상단 구분 (대각선 기준)
            // 기울기 계산: y / adjustedGridSizeY > x / gridSizeX  =>  y * gridSizeX > x * adjustedGridSizeY
            if (y * gridSizeX > x * adjustedGridSizeY) // 근사적으로 대각선 위 (우상단 영역)
            {
                 return GetColorFromBrickLogic(2); // 거의 노란색 (우상단 색상)
            }
            else // 근사적으로 대각선 아래 (좌하단 영역)
            {
                 return new Color(0.5f, 0.8f, 0.5f); // 차분한 초록색 (좌하단 색상)
            }
        }
        else // 기본 4인용 또는 그 외
        {
            // 기존 4분할 로직
            bool isRightHalf = x >= gridSizeX / 2;
            bool isTopHalf = y >= adjustedGridSizeY / 2;

            if (!isRightHalf && isTopHalf) // 좌상단
            {
                // return new Color(1f, 0.7f, 0.7f); // 이전: 채도 낮은 빨간색
                return GetColorFromBrickLogic(28); // 거의 빨간색
            }
            else if (isRightHalf && isTopHalf) // 우상단
            {
                // return new Color(1f, 1f, 0.7f); // 이전: 채도 낮은 노란색
                return GetColorFromBrickLogic(2); // 거의 노란색
            }
            else if (!isRightHalf && !isTopHalf) // 좌하단
            {
                // return new Color(0.7f, 1f, 0.7f); // 이전: 채도 낮은 초록색
                return new Color(0.5f, 0.8f, 0.5f); // 차분한 초록색 (로직과 별개)
            }
            else // 우하단
            {
                // return new Color(1f, 0.8f, 0.6f); // 이전: 채도 낮은 주황색
                return GetColorFromBrickLogic(15); // 주황색
            }
        }
    }

    // 각 플레이어의 색상 정의
    private void DefinePlayerColors()
    {
        _playerColors.Clear();
        
        // 플레이어 수에 따라 색상 정의
        if (playerCount == 2)
        {
            // *** 플레이어 0과 1의 색상 교체 ***
            // 이전: 0(초록), 1(노랑)
            // 변경: 0(노랑), 1(초록)
             _playerColors[0] = GetColorFromBrickLogic(2); // 노란색 - 플레이어 0
             _playerColors[1] = new Color(0.5f, 0.8f, 0.5f); // 초록색 - 플레이어 1
        }
        else // 4인용 또는 그 외
        {
            // 4인용은 그대로 유지
            _playerColors[0] = new Color(1.0f, 0.0f, 0.0f); // 빨강 (좌하단)
            _playerColors[1] = new Color(1.0f, 0.8f, 0.0f); // 노랑 (우상단)
            _playerColors[2] = new Color(0.0f, 0.6f, 0.0f); // 초록 (좌상단)
            _playerColors[3] = new Color(0.5f, 0.3f, 1.0f); // 보라 (우하단)
        }
        
        // 디버그 로그로 플레이어 색상 출력
        for (int i = 0; i < (playerCount == 2 ? 2 : 4); i++)
        {
            if (_playerColors.TryGetValue(i, out Color color))
            {
                Debug.Log($"<color=cyan>[IsometricGridGenerator] 플레이어 {i}의 색상: R:{color.r}, G:{color.g}, B:{color.b}</color>");
            }
        }
    }
    
    // 초기 블록 소유자 ID 결정 
    private int GetInitialOwnerID(int x, int y, int width, int height)
    {
        // 플레이어 수에 따라 초기 소유권 다르게 설정
        if (playerCount == 2)
        {
            // *** 플레이어 0과 1의 영역 교체 ***
            // 기울기 계산: y / height > x / width  =>  y * width > x * height
            if (y * width > x * height) // 근사적으로 대각선 위 (원래 플레이어 1 영역)
            {
                return 1; // 이제 플레이어 1 영역
            }
            else // 근사적으로 대각선 아래 (원래 플레이어 0 영역)
            {
                return 0; // 이제 플레이어 0 영역
            }
        }
        else // 4인용
        {
           // 4인용은 그대로 유지
            bool isRightHalf = x >= width / 2;
            bool isTopHalf = y >= height / 2;

            if (!isRightHalf && isTopHalf) // 좌상단
            {
                return 2;
            }
            else if (isRightHalf && isTopHalf) // 우상단
            {
                return 1;
            }
            else if (!isRightHalf && !isTopHalf) // 좌하단
            {
                return 0;
            }
            else // 우하단
            {
                return 3;
            }
        }
    }

    // 터렛 인스턴스화 헬퍼 함수 (플레이어 ID 추가)
    void InstantiateTurret(GameObject prefab, Vector3 position, Transform parent, int playerID = -1)
    {
        // 터렛 생성
        GameObject turretGO = Instantiate(prefab, position, Quaternion.identity, parent);
        
        // Cannon 컴포넌트 확인 및 리스트에 추가
        Cannon cannon = turretGO.GetComponent<Cannon>();
        if (cannon != null)
        {
            _cannons.Add(cannon);
            
            // 플레이어 ID 설정
            cannon.playerID = playerID;
            
            // 플레이어 색상 설정 (시각적 구분용)
            if (_playerColors.TryGetValue(playerID, out Color color))
            {
                Renderer renderer = cannon.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
                
                // 플레이어 색상 저장
                cannon.playerColor = color;
            }
            
            Debug.Log($"<color=cyan>[IsometricGridGenerator] 플레이어 {playerID}의 캐논 생성: {turretGO.name}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[IsometricGridGenerator] 생성된 터렛에 Cannon 컴포넌트가 없습니다: {turretGO.name}</color>");
        }
    }
    
    // ReleaseGameManager가 호출할 수 있는 모든 캐논 가져오기 메서드
    public Cannon[] GetAllCannons()
    {
        return _cannons.ToArray();
    }

    // 블록 소유권 설정 메서드
    public bool SetBlockOwner(GameObject block, int playerID, Color playerColor)
    {
        if (block == null) return false;
        
        // 블록 소유권 업데이트
        _blockOwners[block] = playerID;
        
        // 블록 색상 변경 (이미 Renderer에서 설정했지만 백업으로 유지)
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }
        
        Debug.Log($"<color=magenta>[IsometricGridGenerator] 블록 {block.name}의 소유권이 플레이어 {playerID}로 변경됨</color>");
        return true;
    }
    
    // 특정 플레이어가 소유한 블록 수 반환
    public int GetBlockCountByPlayer(int playerID)
    {
        int count = 0;
        foreach (var pair in _blockOwners)
        {
            if (pair.Value == playerID)
                count++;
        }
        return count;
    }
    
    // 모든 블록 수 반환
    public int GetTotalBlockCount()
    {
        return gridSizeX * Mathf.RoundToInt(gridSizeY * aspectRatio);
    }
    
    // 플레이어 색상 가져오기
    public Color GetPlayerColor(int playerID)
    {
        if (_playerColors.TryGetValue(playerID, out Color color))
            return color;
            
        return Color.white; // 기본 색상
    }
    
    // 블록 소유자 ID 가져오기
    public int GetBlockOwner(GameObject block)
    {
        if (block == null) return -1;
        
        if (_blockOwners.TryGetValue(block, out int ownerID))
            return ownerID;
            
        return -1; // 소유자 없음 (중립)
    }
}