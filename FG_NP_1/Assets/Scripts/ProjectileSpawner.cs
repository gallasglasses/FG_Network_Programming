using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class ProjectileSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;

    public void SpawnProjectile(Vector3 position, Quaternion rotation, Vector3 direction)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            var networkObject = NetworkObjectPool.Singleton.GetNetworkObject(_projectilePrefab, position, rotation);
            if(networkObject.TryGetComponent<Projectile>(out Projectile networkProjectile))
            {
                networkProjectile.SetDirection(direction);
                networkProjectile.SetSpawner(this);
            }
            
            networkObject.Spawn(true);
        }
    }

    public void DespawnProjectile(NetworkObject networkObject, GameObject projectilePrefab)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObjectPool.Singleton.ReturnNetworkObject(networkObject, _projectilePrefab);
            networkObject.Despawn(false);
        }
    }

    //public ObjectPool<Projectile> pool;
    //[SerializeField] private int spawnCount = 20;

    //private List<Projectile> activeProjectiles = new List<Projectile>();
    //private WeaponComponent weaponComponent;
    //private bool isReturningActiveObjectsToPool = false;

    //private void Awake()
    //{
    //    SceneManager.sceneLoaded += OnSceneLoaded;
    //}

    //private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    //{
    //    if (scene.buildIndex == 0)
    //        return;
    //}
    //public void ReturnActiveObjectsToPool()
    //{
    //    if (activeProjectiles != null && activeProjectiles.Count > 0)
    //    {
    //        isReturningActiveObjectsToPool = true;
    //        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
    //        {
    //            if (activeProjectiles[i] != null && activeProjectiles[i].gameObject.activeSelf)
    //            {
    //                Debug.Log($"activeProjectiles {activeProjectiles[i]}");
    //                pool.Release(activeProjectiles[i]);
    //            }
    //            else
    //            {
    //                activeProjectiles.RemoveAt(i);
    //            }
    //        }
    //        activeProjectiles.Clear();
    //    }
    //}

    //void Start()
    //{
    //    isReturningActiveObjectsToPool = false;
    //    CreatePool();
    //}
    //private void CreatePool()
    //{
    //    weaponComponent = GetComponent<WeaponComponent>();
    //    pool = new ObjectPool<Projectile>(CreateProjectile, OnTakeProjectileFromPool, OnReturnProjectileToPool, OnDestroyProjectile, true, spawnCount, spawnCount);
    //}

    //private Projectile CreateProjectile()
    //{
    //    var projectile = Instantiate(weaponComponent.projectile, weaponComponent.transform.position, Quaternion.identity);
    //    if (projectile != null)
    //    {
    //        projectile.SetPool(pool);
    //    }

    //    if (projectile.TryGetComponent<NetworkObject>(out NetworkObject networkProjectile))
    //    {
    //        networkProjectile.Spawn();
    //    }
    //    return projectile;
    //}

    //private void OnTakeProjectileFromPool(Projectile projectile)
    //{
    //    projectile.gameObject.SetActive(true);
    //    activeProjectiles.Add(projectile);
    //}

    //private void OnReturnProjectileToPool(Projectile projectile)
    //{
    //    projectile.gameObject.SetActive(false);
    //    if (activeProjectiles.Contains(projectile))
    //    {
    //        activeProjectiles.Remove(projectile);
    //        if (isReturningActiveObjectsToPool)
    //        {
    //            Destroy(projectile.gameObject);
    //        }
    //    }
    //}

    //private void OnDestroyProjectile(Projectile projectile)
    //{
    //    Destroy(projectile.gameObject);
    //}

    //private void OnDisable()
    //{
    //    ReturnActiveObjectsToPool();
    //    SceneManager.sceneLoaded -= OnSceneLoaded;
    //}
}
