using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Add reference to the Ball script
    public Ball ball;

    private const float BLOCK_SPEED_INCREASE_RATE = 1.2F;

    public static float BLOCK_SPEED = 0.05F;

    public Transform startingTransform = null;
    public static int score = 0;
    public static int level = 1;

    // public TextMeshProUGUI gameOverScoreText = null;
    public static int budged = 40;

    // [SerializeField] private GameObject pauseMenu = null;
    // [SerializeField] private GameObject gameOverMenu = null;
    [SerializeField] private GameObject blockPrefab = null;
    // [SerializeField] private GameObject settingsMenu = null;
    // [SerializeField] private GameObject popupPanel = null;
    // [SerializeField] private Text levelText = null;
    // [SerializeField] private TextMeshProUGUI budgedText = null;

    private MaterialInitilizer materialInitilizer;
    // private AudioSource audioSource;

    [Header("Brick Grid Settings")]
    public int rows = 8;
    public int columns = 10;
    public float horizontalSpacing = 1.2f;
    public float verticalSpacing = 0.6f;
    public Transform gridStartPosition;

    void Start()
    {
        materialInitilizer = GetComponent<MaterialInitilizer>();
        // audioSource = GetComponent<AudioSource>();
        // levelText.text = "Level " + level.ToString();
        // levelText.GetComponent<Animation>().Play();
        Time.timeScale = 1;

        GenerateBrickGrid();

        // Automatically launch the ball at the start
        if (ball != null)
        {
            // Define launch direction (e.g., upwards) and force
            // Vector3 launchDirection = Vector3.up + Vector3.right * 0.2f; // Slightly angled up-right
            // float launchForce = 5f; // Adjust force as needed
            // ball.Launch(launchDirection.normalized, launchForce);
        }
        else
        {
            Debug.LogError("GameManager: Ball reference is not set in the Inspector!");
        }
    }


    void Update()
    {
        // budgedText.text = budged.ToString();
    }

    void GenerateBrickGrid()
    {
        if (blockPrefab == null || gridStartPosition == null)
        {
            Debug.LogError("GameManager: Block Prefab 또는 Grid Start Position이 설정되지 않았습니다!");
            return;
        }

        Vector3 startPos = gridStartPosition.position;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 spawnPosition = startPos + new Vector3(col * horizontalSpacing, -row * verticalSpacing, 0);

                GameObject brickGO = Instantiate(blockPrefab, spawnPosition, Quaternion.identity);

                Brick brickScript = brickGO.GetComponent<Brick>();
                if (brickScript != null)
                {
                    brickScript.health = (row + 1) * 2;
                }
                else
                {
                    Debug.LogError("GameManager: 생성된 벽돌에 Brick 스크립트가 없습니다!");
                }
            }
        }
    }

    public void instantiateBlock()
    {
        level++;
        // levelText.text = "Level " + level.ToString();
        // levelText.GetComponent<Animation>().Play();
        // audioSource.Play();
        BLOCK_SPEED = BLOCK_SPEED * BLOCK_SPEED_INCREASE_RATE;
        // Vector3 spawnPos = ... // 새로운 벽돌 생성 위치 계산 필요
        // GameObject block = Instantiate(blockPrefab, spawnPos, startingTransform.rotation);
        // materialInitilizer.changeBlockMaterial(block);
        Debug.LogWarning("instantiateBlock() called - implement specific logic if needed for level progression.");
    }

    public void openPopupPanel(Vector3 position, int value, Color color)
    {
        // popupPanel.GetComponent<CanvasGroup>().alpha = 1;
        // popupPanel.transform.position = position;
        // string prefix = color.Equals(Color.green) ? "+" : "-";
        // popupPanel.transform.Find("text").GetComponent<TextMeshProUGUI>().text = prefix + value.ToString();
        // popupPanel.transform.Find("text").GetComponent<TextMeshProUGUI>().color = color;
        // popupPanel.GetComponent<Animation>().Play("PopupCloseAnim");
    }


    public void openGameOverMenu()
    {
        // gameOverMenu.SetActive(true);
        // gameOverScoreText.text = score.ToString();
    }

    public void openSettings()
    {
        // settingsMenu.SetActive(true);

    }


    public void resume()
    {
        Time.timeScale = 1;
        // pauseMenu.SetActive(false);
    }

    private void pause()
    {
        Time.timeScale = 0;
        // pauseMenu.SetActive(true);
    }

    public void mainMenu()
    {
        // SceneManager.LoadScene("HomeScene");
    }









}
