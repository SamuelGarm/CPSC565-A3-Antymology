using Antymology.Terrain;
using System;
using UnityEngine;

public class antController : MonoBehaviour
{
    
    public WorldManager world;

    byte[] values;
    int maxHealth = 100;
    int currentHealth = 0;
    int healthDec = -1;

    private Vector3Int blockPos = Vector3Int.zero;

    enum antType { WORKER, QUEEN }
    antType type;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        blockPos = Vector3Int.FloorToInt(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        int y = 0;
        while (world.GetBlock(blockPos.x, y, blockPos.z) is not AirBlock)
        {
            y++;
        }
        blockPos.y = y;
        transform.position = blockPos - new Vector3(0,0.5f,0);
    }

    bool digBelow()
    {
        return true;
    }
}
