using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    public GameObject blockPrefab;
    public float sizeX = 20;
    public float sizeY = 20;

    public int count_x = 30;    // Inspector���� �� ���̰� �ڵ��� ���� ����
    public int count_y = 30;    // Inspector���� �� ���̰� �ڵ��� ���� ����
    public float blockScale = 10f;
    float spacing = 1;
    void Start()
    {
        spacing = sizeX / count_x;
        Vector3 offset = new Vector3((count_x - 1.0f) / 2, (count_y - 1.0f) / 2)  * spacing;
        for (int x = 0; x < count_x; x++)
        {
            for (int y = 0; y < count_y; y++)
            {
                GameObject g = Instantiate(blockPrefab, transform.position - offset + new Vector3(x * spacing, y * spacing, 0), Quaternion.identity);
                g.transform.localScale *= spacing;
                //block.transform.localScale = new Vector3(blockScale, blockScale, 1f);
                //block.transform.parent = transform;
            }
        }
    }
}