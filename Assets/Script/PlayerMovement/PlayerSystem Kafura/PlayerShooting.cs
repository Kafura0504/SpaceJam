using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference mousePos;
    public InputActionReference reload;

    [Header("Shooting")]
    public Transform firepoint;
    public GameObject bullet;

    [Header("Runtime")]
    public Vector2 mouseWorldPos;

    public float fireRate = 8f;

    private float fireCooldown = 0f;

    public int maxMagazine;
    public int currentMag;

    private PlayerStat stat;
    private bool reloading = false;
    public float ReloadSpd;

    void Start()
    {
        stat = GetComponent<PlayerStat>();

        stat.OnStatChanged += RefreshStat;

        RefreshStat();
        currentMag = maxMagazine;
    }
    void OnEnable()
    {
        reload.action.performed += Reload;
    }

    void OnDisable()
    {
        reload.action.performed -= Reload;
    }

    void Reload(InputAction.CallbackContext ctx)
    {
        if (!reloading && currentMag!=maxMagazine)
        {
            StartCoroutine(Reloading());
        }
    }

    IEnumerator Reloading()
    {
        reloading = true;
        yield return new WaitForSeconds(ReloadSpd);
        currentMag = maxMagazine;
        reloading = false;
    }
    void OnDestroy()
    {
        stat.OnStatChanged -= RefreshStat;
    }

    void RefreshStat()
    {
        maxMagazine = stat.magazine;

        currentMag = Mathf.Clamp(currentMag, 0, maxMagazine);

        fireRate = stat.fireRate;
    }

    void Update()
    {
        AimToMouse();

        HandleShooting();
        if (!reloading && currentMag <=0)
        {
            StartCoroutine(Reloading());
        }
    }

    void AimToMouse()
    {
        Vector2 mouseScreenPos =
            mousePos.action.ReadValue<Vector2>();

        mouseWorldPos =
            Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector2 direction =
            mouseWorldPos - (Vector2)transform.position;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // IF SPRITE FACES UP
        transform.rotation =
            Quaternion.Euler(0, 0, angle - 90f);
    }

    void HandleShooting()
    {
        if (fireCooldown > 0f)
            fireCooldown -= Time.deltaTime;

        if (Input.GetMouseButton(0) &&
            fireCooldown <= 0f &&
            currentMag > 0 && !reloading)
        {
            Shoot();

            currentMag--;

            fireCooldown = 1f / fireRate;
        }
    }

    void Shoot()
    {
        GameObject spawnedBullet =
            Instantiate(
                bullet,
                firepoint.position,
                firepoint.rotation
            );

        BulletP bulletData =
            spawnedBullet.GetComponent<BulletP>();

        bulletData.damage = stat.damage;

        bulletData.speed = stat.bulletVelocity;
 bulletData.SetDirection(firepoint.up);
}
}