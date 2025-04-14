using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Trigger : MonoBehaviour
{
    public enum TriggerType { multiply , release }
    public TriggerType type;
    public Transform spawnPoint;
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
        Marble marble = collision.gameObject.GetComponent<Marble>();
        if (marble != null) {
            switch (type)
            {
                case TriggerType.multiply :
                    marble.player.currnetNumber = marble.player.currnetNumber * 5;
                    break;
                case TriggerType.release:
                    marble.player.ammoLeft += marble.player.currnetNumber;
                    marble.player.currnetNumber = 2;
                    break;
            }
            marble.transform.position = marble.player.spawnPoint.position;
            //marble.transform.position = spawnPoint.position;
        }
    }
}
