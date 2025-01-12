using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using UnityEngine.EventSystems;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float force = 10f;
    [SerializeField] private float deactivateTime = 5f;

    private Rigidbody rb;
    private Vector3 direction;

    private ProjectileSpawner spawner;
    //private ObjectPool<Projectile> pool;
    private Coroutine deactivateProjectileAfterTimeCoroutine;

    private bool _hasBeenReleased = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        deactivateProjectileAfterTimeCoroutine = StartCoroutine(DeactivateProjectileAfterTime());

        _hasBeenReleased = false;
    }

    private void FixedUpdate()
    {
        //SetVelocity();
        if(IsServer)
        {
            //UpdateProjectileClientRpc(transform.position);
            transform.position += direction * Time.fixedDeltaTime;
        }
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized * force;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasBeenReleased) return;

        _hasBeenReleased = true;
        if (TryGetComponent<NetworkObject>(out NetworkObject networkProjectile))
        {
            spawner.DespawnProjectile(networkProjectile, gameObject);
        }
        //pool.Release(this);
    }

    public void SetSpawner(ProjectileSpawner s)
    {
        spawner = s;
    }

    //public void SetPool(ObjectPool<Projectile> p)
    //{
    //    pool = p;
    //}

    private IEnumerator DeactivateProjectileAfterTime()
    {
        float elapsedTime = 0f;
        while(elapsedTime < deactivateTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!_hasBeenReleased) 
        {
            _hasBeenReleased = true;

            if (TryGetComponent<NetworkObject>(out NetworkObject networkProjectile))
            {
                spawner.DespawnProjectile(networkProjectile, gameObject);
            }
            //pool.Release(this);
        }
    }

    [ClientRpc]
    public void UpdateProjectileClientRpc(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    //public void SetVelocity()
    //{
    //    rb.linearVelocity = direction.normalized * force; 
    //}
}
