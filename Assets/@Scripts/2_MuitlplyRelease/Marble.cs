using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marble : MonoBehaviour
{
    public MuitlplyRelease_Player player;
    public SpriteRenderer spriteRenderer;
    public TrailRenderer trailRenderer;
    public Rigidbody2D rigidbody;
    // Start is called before the first frame update
    void Start()
    {
        //spriteRenderer.color = player.bulletColor;
        //trailRenderer.startColor = player.bulletColor;
        //Color c = player.bulletColor;
        //c.a = 0;
        //trailRenderer.endColor = c;
    }

    public void Setup(MuitlplyRelease_Player p)
    {
        player = p;
        spriteRenderer.color = player.bulletColor;
        trailRenderer.startColor = player.bulletColor;
        Color c = player.bulletColor;
        c.a = 0;
        trailRenderer.endColor = c;

        spriteRenderer.color = player.bulletColor;
        rigidbody.linearVelocity = new Vector2(Random.value, Random.value);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
