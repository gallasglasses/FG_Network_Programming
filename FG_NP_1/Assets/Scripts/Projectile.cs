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
    private NetworkVariable<Vector3> networkDirection = new NetworkVariable<Vector3>();
    private ProjectileSpawner spawner;
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
        direction = Vector3.zero;
    }

    private void FixedUpdate()
    {
        transform.position += direction * Time.fixedDeltaTime;
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized * force;
        networkDirection.Value = direction;
        if (rb != null)
        {
            rb.AddForce(direction);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasBeenReleased) return;

        GamePlayer player = other.GetComponent<GamePlayer>();
        if (player != null)
        {
            player.Stun();
            _hasBeenReleased = true;
            if (TryGetComponent<NetworkObject>(out NetworkObject networkProjectile))
            {
                spawner.DespawnProjectile(networkProjectile, gameObject);
            }
        }
    }

    public void SetSpawner(ProjectileSpawner s)
    {
        spawner = s;
    }

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
        }
    }
}
