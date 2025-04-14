using UnityEngine;

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

    public static IsometricGridGenerator Instance { get; private set; }

    void Awake() // Start 대신 Awake 사용 권장
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

            // 기존 Start 내용 (borderMaterial 체크 등)은 Awake나 Start 어디든 두어도 괜찮으나,
            // Instance 설정 후에 다른 스크립트가 접근할 수 있도록 Awake가 더 일반적입니다.
            if (borderMaterial == null)
            {
                 Debug.LogError("Border Material이 Inspector에서 할당되지 않았습니다!");
                 enabled = false;
                 return;
            }
            // CreateGrid() 호출은 Start에서 해도 됩니다.
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
        // Clear existing objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
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
                    cube.tag = "GridBlock";
                }
                
                cube.transform.localScale = new Vector3(cubeSize, gridHeight, cubeSize);
                cube.transform.parent = this.transform;
                
                Renderer renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Adjust the color calculation to account for the new dimensions
                    Color cubeColor = GetCubeColor(x, y, adjustedGridSizeY);
                    renderer.material.color = cubeColor;
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

            if (playerCount == 2)
            {
                // 좌상단, 우하단에 배치 (-> 수정: 좌상단, 우하단)
                // InstantiateTurret(standardTurretPrefab, bottomLeftPos, transform); // 이전
                // InstantiateTurret(standardTurretPrefab, topRightPos, transform); // 이전
                InstantiateTurret(standardTurretPrefab, topLeftPos, transform); // 수정: 좌상단
                InstantiateTurret(standardTurretPrefab, bottomRightPos, transform); // 수정: 우하단
            }
            else // 4인 또는 그 외
            {
                // 네 모서리에 모두 배치
                InstantiateTurret(standardTurretPrefab, bottomLeftPos, transform);
                InstantiateTurret(standardTurretPrefab, topRightPos, transform);
                InstantiateTurret(standardTurretPrefab, topLeftPos, transform);
                InstantiateTurret(standardTurretPrefab, bottomRightPos, transform);
            }
        }
        // --- 터렛 배치 로직 끝 ---
        
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

    // 터렛 인스턴스화 헬퍼 함수
    void InstantiateTurret(GameObject prefab, Vector3 position, Transform parent)
    {
         Instantiate(prefab, position, Quaternion.identity);

    }
}