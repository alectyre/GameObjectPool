using System.Collections.Generic;
using UnityEngine;
using Pooling;

public class ObjectPoolTest : MonoBehaviour
{
    [SerializeField] private GameObjectPool objectPool;

    private static Queue<GameObject> objectsFromPool = new Queue<GameObject>();
    private static ObjectPoolTest activeObjectPoolTest;


    private void OnEnable()
    {
        activeObjectPoolTest = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && activeObjectPoolTest == this)
        {
            Debug.Log("Spawning PooledGameObject");
            GameObject pooledGameObject = objectPool.GetPooledObject(gameObject.scene.buildIndex);

            if (pooledGameObject != null)
            {
                Vector3 screenPosition = Camera.main.ScreenToWorldPoint(new Vector3(Random.Range(0, Screen.width), Random.Range(0, Screen.height), Camera.main.farClipPlane / 2));

                pooledGameObject.transform.position = screenPosition;
                pooledGameObject.transform.rotation = Random.rotation;
                
                objectsFromPool.Enqueue(pooledGameObject);
            }
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && activeObjectPoolTest == this)
        {
            if(objectsFromPool.Count > 0)
                objectPool.ReturnToPool(objectsFromPool.Dequeue());
        }
    }

    private void OnDisable()
    {
       if(activeObjectPoolTest = this)
            activeObjectPoolTest = null;
    }
}
