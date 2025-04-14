using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MuitlplyRelease_Player : MonoBehaviour
{
    public Color bulletColor;
    public Color blockColor;

    public int currnetNumber = 1;
    public int ammoLeft = 0;
    public Transform barrel;
    public Transform spawnPoint;

    public GameObject bulletPrefab;
    public GameObject marblePrefab;
    public TMP_Text label;

    float cooldownLeft;

    public float marbleSpawnCooldown = 60;
    float marbleSpawnCooldownLeft = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        label.text = currnetNumber.ToString();
        cooldownLeft -= Time.deltaTime;
        //if (ammoLeft > 0)

        //if (ammoLeft > 0 && cooldownLeft <= 0)
        if (ammoLeft > 0)
        {

            cooldownLeft = 0.05f;

            GameObject g =  Instantiate(bulletPrefab, barrel.position, Quaternion.identity);
            Bullet bullet = g.GetComponent<Bullet>();
            bullet.rigidbody.linearVelocity = barrel.right * 10;
            bullet.spriteRenderer.color = bulletColor;
            bullet.player = this;
            ammoLeft--;
        }

        marbleSpawnCooldownLeft -= Time.deltaTime;
        if (marbleSpawnCooldownLeft <= 0)  // marbleSpawnCooldownLeft를 체크해야 합니다
        {
            marbleSpawnCooldownLeft = marbleSpawnCooldown;
            GameObject g = Instantiate(marblePrefab, spawnPoint.position, Quaternion.identity);
            Marble m = g.GetComponent<Marble>();
            m.Setup(this);
        }
    }
}
