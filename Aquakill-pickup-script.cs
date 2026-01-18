using System.Collections.Generic;
using UnityEngine;

public abstract class Pickup : MonoBehaviour
{

    [Header("Collider Values for its init.")]
    private CapsuleCollider _collider;
    private SphereCollider _physicsCollider;
    private Rigidbody _rb;
    private float _colliderRadius = 1.4f;
    private float _colliderHeight = 3f;
    private Vector3 _colliderCenter = new Vector3(0,2f,0);
    public int value;
    private bool _used = false;
    [Header("Sounds")]
    public AudioClip pickupSound;
    public float volume = 1f;
    public bool fallsOnSpawn = true;
    private ObjectShaderHandler _dissolve;

    void Awake()
    {
        TriggerSetUp();
        _dissolve = GetComponent<ObjectShaderHandler>();
    }

    private void TriggerSetUp()
    {

        _collider = transform.GetComponent<CapsuleCollider>();
        if (_collider == null )
        {
            gameObject.AddComponent<CapsuleCollider>();
            _collider = transform.GetComponent<CapsuleCollider>();
        }

        _collider.radius = _colliderRadius;
        _collider.height = _colliderHeight;
        _collider.center = _colliderCenter;
        _collider.enabled = true;
        _collider.isTrigger = true;
        _collider.includeLayers = LayerMask.GetMask("Player");
        _collider.excludeLayers = LayerMask.GetMask("Projectile", "Enemy", "Pickup");
        gameObject.AddComponent<SphereCollider>();
        _physicsCollider = transform.GetComponent<SphereCollider>();
        _physicsCollider.radius = _colliderRadius;
        _physicsCollider.center = _colliderCenter;

        _physicsCollider.enabled = true;
        _physicsCollider.isTrigger = false;
        _physicsCollider.includeLayers = LayerMask.GetMask("Ground", "Default");
        _physicsCollider.excludeLayers = LayerMask.GetMask("Projectile", "Enemy", "Player", "Pickup");
        gameObject.AddComponent<Rigidbody>();
        _rb = transform.GetComponent<Rigidbody>();
        _rb.useGravity = true;
        _rb = transform.GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.angularDamping = 4;
        _rb.mass = 1;
        if(!fallsOnSpawn)
        {
            _rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        gameObject.transform.rotation = Quaternion.identity;


    }

    private void OnCollisionEnter(Collision collision)
    {
        _rb.constraints = RigidbodyConstraints.FreezeAll;
        _physicsCollider.enabled = false;
    }
    /*!
     * Gives the player more ammo for the specified weapon in equal to the value of the pickup. Still needs a way to limit max ammo?
     */
    public void GiveAmmo(List<GameObject> guns, AmmoType ammoType, bool specialAmmo)
    {
        if (_used) return;
        foreach (GameObject gun in guns)
        {

            if (gun.GetComponent<Gun>().stats.ammo == ammoType)
            {

                Gun script = gun.GetComponent<Gun>();
                script.AddAmmo(value, specialAmmo);


                //! We destroy the pickup after use

            }

        }
        _used = true;


    }

    public void GiveHealth(PlayerManager manager)
    {
        if (_used) return;
        if (manager.currentHealth == manager.fullHealth) return;

        manager.currentHealth = manager.currentHealth + value;
        if (manager.currentHealth > manager.fullHealth) manager.currentHealth = manager.fullHealth;
        _used = true;
        PickedUp();
    }

    public void GiveGun(PlayerManager manager, WeaponType pickupType, bool isAmmoSpecial)
    {
        WeaponHandler handler = manager.weaponHandler;
        if (_used) return;
        if (manager.foundGuns.Contains(pickupType))
        {
            PickedUp();
            _used = true;
            GiveHealth(manager);
            return;

        }
        handler.AddWeapon(pickupType);
        _used = true;
        PickedUp();
    }
    
    public virtual void PickedUp()
    {
        //! Sound
        SoundFXManager.Instance.PlayPitchedSoundFXClip(pickupSound, transform, volume);
        if(_dissolve != null)
        {
            StartCoroutine(_dissolve.Dissolve());
        }
        else
        {
            Destroy(gameObject);
        }

    }
}
