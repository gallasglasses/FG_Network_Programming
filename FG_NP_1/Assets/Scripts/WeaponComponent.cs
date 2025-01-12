using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class WeaponComponent : NetworkBehaviour
{
    public Projectile projectile;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float cooldown;

    private ProjectileSpawner projectileSpawner;
    private Vector3 projectileDirection;
    private bool canFire;
    private bool isReadyAttack;
    private float timer;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void Start()
    {
        if (projectileSpawner == null)
            projectileSpawner = GetComponent<ProjectileSpawner>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
            return;
    }

    void Update()
    {
        if (!canFire)
        {
            timer += Time.deltaTime;
            if (timer > cooldown)
            {
                canFire = true;
                timer = 0;
            }
        }

        if (isReadyAttack && canFire)
        {
            canFire = false;
            Attack();
        }
    }

    public void HandleAttack(bool canStart, Vector3 dir)
    {
        isReadyAttack = canStart;
        projectileDirection = dir;
    }

    private void Attack()
    {
        canFire = false;

        StartShooting();
    }

    private void StartShooting()
    {
        Vector3 spawnPosition = transform.position + projectileDirection * spawnRadius;
        if (IsServer)
        {
            projectileSpawner.SpawnProjectile(spawnPosition, Quaternion.identity, projectileDirection);
            //projectile.SetDirection(projectileDirection);
            //projectile.GetComponent<NetworkObject>().Spawn(true);
        }
        else if (IsClient && IsOwner)
        {
            SpawnProjectileServerRpc(spawnPosition, projectileDirection);
        }

        isReadyAttack = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnProjectileServerRpc(Vector3 position, Vector3 direction)
    {
        projectileSpawner.SpawnProjectile(position, Quaternion.identity, direction);

        //var projectile = projectileSpawner.pool.Get();
        //projectile.transform.position = position;
        //projectile.transform.rotation = Quaternion.identity;
        //projectile.SetDirection(direction);

        //projectile.GetComponent<NetworkObject>().Spawn(true);
        //projectile.UpdateProjectileClientRpc(position);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
