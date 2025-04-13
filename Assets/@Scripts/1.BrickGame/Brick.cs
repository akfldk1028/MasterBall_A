using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    public int health = 5; // 벽돌의 초기 체력 (숫자)
    public GameObject itemPrefab; // 떨어뜨릴 아이템 프리팹

    private const int COPPER_CHANCE = 50;
    private const int SILVER_CHANCE = 30;
    private const int GOLD_CHANCE = 10;

    // [SerializeField] private GameObject[] coinPrefabs = null;

    void Start()
    {
        UpdateHealthText(); // 게임 시작 시 초기 체력 표시
    }

    void OnCollisionEnter2D(Collision2D collision) // 2D 충돌 감지
    {
        if (collision.gameObject.CompareTag("Ball")) // 충돌한 오브젝트가 공(Ball) 태그를 가졌는지 확인
        {
            health--; // 체력 감소
            UpdateHealthText(); // 숫자 텍스트 업데이트

            if (health <= 0)
            {
                DestroyBrick(); // 체력이 0 이하이면 벽돌 파괴
            }
        }
    }

    void UpdateHealthText()
    {

    }

    void DestroyBrick()
    {
        if (itemPrefab != null)
        {
            // 아이템 생성 (위치는 벽돌 위치)
            Instantiate(itemPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject); // 벽돌 오브젝트 파괴
    }

    void Update()
    {
        if (transform.childCount == 0)
        {

            // int random = Random.Range(0, 50);

            // if (random < GOLD_CHANCE)
            // {
            //     Instantiate(coinPrefabs[0], transform.position, coinPrefabs[0].transform.rotation);
            // }
            // else if (random < SILVER_CHANCE)
            // {

            //     Instantiate(coinPrefabs[1], transform.position, coinPrefabs[1].transform.rotation);
            // }
            // else if (random < GOLD_CHANCE)
            // {
            //     Instantiate(coinPrefabs[2], transform.position, coinPrefabs[2].transform.rotation);
            // }

            // Destroy(gameObject);
        }
    }
}
