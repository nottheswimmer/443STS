using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject[] objects;                // The prefab to be spawned.
    public float spawnTime = 6f;            // How long between each spawn.
    private Vector3 spawnPosition;
    public int spawnLimit = 3;
    public Transform parent;
 
    // Use this for initialization
    void Start () 
    {
        // Call the Spawn function after a delay of the spawnTime and then continue to call after the same amount of time.
        InvokeRepeating ("Spawn", spawnTime, spawnTime);
     
    }
 
    void Spawn ()
    {
        spawnPosition.x = Random.Range (-17, 17);
        spawnPosition.y = 20f;
        spawnPosition.z = Random.Range (-17, 17);
        if (spawnLimit > parent.childCount)
        {
            Instantiate(objects[UnityEngine.Random.Range(0, objects.Length - 1)], spawnPosition, Quaternion.identity, 
                parent);
        }
    }
}
