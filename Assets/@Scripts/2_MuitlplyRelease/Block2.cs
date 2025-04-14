using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block2 : MonoBehaviour
{
    public MuitlplyRelease_Player player;
    public SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Bullet bullet = collision.gameObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            if (bullet.player != player & bullet.hasBeenUsed == false) 
            { 
                player = bullet.player;
                spriteRenderer.color = player.blockColor;
                bullet.hasBeenUsed = true;
                Destroy(bullet.gameObject);
            }
        }
    }
}
