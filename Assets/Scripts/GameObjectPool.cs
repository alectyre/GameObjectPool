using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pooling
{
    /// <summary>
    /// Manages a cross-scene pool of instances of a prefab. Pooled objects are considered returned to the pool while inactive.
    /// </summary>
    [CreateAssetMenu(fileName = "New GameObjectPool", menuName = "Create GameObjectPool")]
    public class GameObjectPool : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [Tooltip("No size limit if negative.")]
        [SerializeField] private int maxSize = -1;

        private List<PooledObjectData> pooledObjects;
        private int currentPoolIndex;
        
        [System.Serializable]
        private struct PooledObjectData
        {
            public GameObject GameObject;
            public int SceneIndex;
            public float RetrievalTime;

            public PooledObjectData(GameObject gameObject, int sceneIndex)
            {
                GameObject = gameObject;
                SceneIndex = sceneIndex;
                RetrievalTime = float.MaxValue;
            }
        }

        public GameObject Prefab { get { return prefab; } }

        public int MaxSize { get { return maxSize; } set { maxSize = value; } }

        private void OnEnable()
        {
            //Prevents the asset from being unloaded when there are no scene references to it
            hideFlags = HideFlags.DontUnloadUnusedAsset;

            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (pooledObjects != null)
            {
                for (int i = 0; i < pooledObjects.Count; i++)
                {
                    PooledObjectData pooledObject = pooledObjects[i];

                    if (pooledObject.SceneIndex == scene.buildIndex)
                    {
                        pooledObject.SceneIndex = -1;
                        
                        if(pooledObject.GameObject != null)
                            pooledObject.GameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnDisable()
        {
            Clear();

            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        /// <summary>
        /// Returns an inactive prefab instance, or instaniates a new one if none is found.
        /// Setting the pooled GameObject to inactive returns it to the pool.
        /// When the scene with the provided index is unloaded, associated pooled objects will be returned to the pool.
        /// </summary>
        /// <param name="sceneIndex">The scene index the pooledObject will be associated with.</param>
        /// <returns>A prefab instance from the pool, or null if all pooled objects in use and pool is at max size.</returns>
        public GameObject GetPooledObject(int sceneIndex)
        {
            Initialize(1);

            PooledObjectData pooledObject;
            GameObject gameObject = null;

            int index = currentPoolIndex;
            do
            {
                //Loop through pooledObject indices
                index = (index + 1) % pooledObjects.Count;

                pooledObject = pooledObjects[index];
                gameObject = pooledObject.GameObject;

                //Remove any null pooledObject we find
                if (gameObject == null)
                {
                    Debug.LogWarning($"{typeof(GameObjectPool)} {name}: Found destroyed pooled object.");
                    pooledObjects.RemoveAt(index--);

                    //Add one if we empty the pool
                    if (pooledObjects.Count == 0)
                        TryGrowPool(out pooledObject);
                    continue;
                }

                if (gameObject.activeInHierarchy == false)
                {
                    currentPoolIndex = index;
                    break; //If we find an inactive pooledObject, break out of loop
                }
                else
                {
                    gameObject = null; //Otherwise try again
                }
            } while (index != currentPoolIndex); //End loop when we've looped back to currentPoolIndex

            //No pooled object found, try to grow the pool.
            if (gameObject == null && TryGrowPool(out pooledObject))
            {
                gameObject = pooledObject.GameObject;
                index = pooledObjects.Count - 1;
            }

            //If we successfully retrieved a pooled object, set its data and set it active
            if (gameObject != null)
            {
                pooledObject.SceneIndex = sceneIndex;
                pooledObject.RetrievalTime = Time.realtimeSinceStartup;
                pooledObjects[index] = pooledObject;
                gameObject.SetActive(true);
            }

            return gameObject;
        }

        /// <summary>
        /// Returns an inactive prefab instance, or instaniates a new one if none is found.
        /// Setting the pooled GameObject to inactive returns it to the pool.
        /// When the scene with the provided index is unloaded, associated pooled objects will be returned to the pool.
        /// </summary>
        /// <param name="sceneIndex">The scene index the pooled object will be associated with.</param>
        /// <param name="position">Position for the pooled object.</param>
        /// <param name="rotation">Orientation of the pooled object.</param>
        /// <returns>A prefab instance from the pool, or null if all pooled objects in use and pool is at max size.</returns>
        public GameObject GetPooledObject(int sceneIndex, Vector3 position, Quaternion rotation)
        {
            GameObject gameObject = GetPooledObject(sceneIndex);

            if (gameObject != null)
            {
                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
            }

            return gameObject;
        }

        /// <summary>
        /// Merely sets the provided GameObject inactive, but nice for making code more explicit.
        /// </summary>
        /// <param name="gameObject">The GameObject to return to the pool</param>
        public void ReturnToPool(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Initializes the pool if uninitialized, and grows the pool to <param name="poolSize"></param>.
        /// </summary>
        /// <param name="poolSize">The size to grow the pool to</param>
        public void Initialize(int poolSize)
        {
            if (pooledObjects == null)
            {
                if (pooledObjects == null)
                    pooledObjects = new List<PooledObjectData>();
            }

            poolSize = Mathf.Min(poolSize, maxSize);
            for (int i = pooledObjects.Count; i < poolSize; i++)
                TryGrowPool(out PooledObjectData pooledObject);
        }

        /// <summary>
        /// Destroys all pooled objects, including those that are not returned to the pool.
        /// </summary>
        public void Clear()
        {
            if (pooledObjects != null)
            {
                foreach (PooledObjectData pooledObject in pooledObjects)
                {
                    if (pooledObject.GameObject != null)
                    {
                        if (Application.isPlaying)
                            Destroy(pooledObject.GameObject);
                    }
                }

                pooledObjects = null;
            }
        }

        private bool TryGrowPool(out PooledObjectData pooledObject)
        {
            if (pooledObjects.Count >= maxSize)
            {
                pooledObject = new PooledObjectData();
                return false;
            }

            if (pooledObjects == null)
                pooledObjects = new List<PooledObjectData>();

            GameObject gameObject = Instantiate(prefab);
            gameObject.name = prefab.name;
            gameObject.SetActive(false);
            DontDestroyOnLoad(gameObject);

            pooledObject = new PooledObjectData(gameObject, -1);

            pooledObjects.Add(pooledObject);

            return true;
        }
    }
}
