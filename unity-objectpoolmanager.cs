using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;
using Object = UnityEngine.Object;
public class PoolManager : MonoBehaviour
{


    /*!
     * How it works:
     * 
     * 1. Create a Scriptable Object PooledObjectSO that has an enum PoolObjects type, and the GameObject Prefab.
     * PoolObjects enum has an enum for every single item that will be pooled. So when a new one to be pooled is made, it should be
     * added to the enum and a PooledObjectSO should be created for it.
     * 
     * 2. Then a new folder called "Resources" needs to be made into Assets. This is used to find all the PooledObjectSOs when the game starts.
     * 
     * 3. Then inside resources a folder needs to be created where all the SOs for objects are placed, with a name matching
     * the string in code that the CreateObjectDictionary function uses.
     * 
     * 4. Create an enum PoolTypes that should have all the item categories you wish to put the pooled items into in the hierarchy
     * Here they are : PARTICLESYSTEM, GAMEOBJECT, VFXGRAPH, SOUNDS
     * 
     * With these steps done and the manager placed into an empty in the scene, everything should work.
     * 
     * The script creates empties in the hierarchy to hold pooled objects. Every object is pulled from the created dictionarys
     * either with the gameobject itself or the PoolObjects type. There are overload methods for either cases.
     * The script can even create pooled objects from anything else unity uses, like Rigidbodies or Audioclips if the object has those.
     * How to use:
     * 
     * A simple use case to create a projectile based on a PoolObjects type that has been set in a PlayerManager
     * 
     *      GameObject instance = PoolManager.SpawnObject
     *          (
     *          PlayerManager.Instance.currentPlayerAttack.projectileType, < This line could also just be a reference to a prefab
     *          transform.position,                                        < This line could just be a reference to a transform if the object 
     *          transform.rotation                                          is to be parented to some other object, with the transform being the parent.
     *          
     *          );
     *          //After declating rotation,
     *          the PoolType can be declared, otherwise it defaults to GAMEOBJECT.
     *          This should be done if the object should be in another category than GAMEOBJECT.
     *          If the function is given a transform instead of position, this can be ignored
     *          as it is parented to the given object and not the emptyGroup.
     * 
     * 
     * Returning an object is simple:
     *          PoolManager.ReturnObjectToPool(gameObject); < After the gameobject declaration
     *                                                        If it was in some other PoolType category than GAMEOBJECT
     *                                                        It should have that referenced here so it goes in the right place
     * 
     */



    public static string objectsFolderNameInResources = "PoolSO"; //Name of the Folder that has the SOs in resources needs to match this
    public static PoolTypes PoolTypes;
    private GameObject _emptyHolder;
    private static GameObject _gameObjectsEmpty;
    private static GameObject _particleSystemsEmpty;
    private static GameObject _vfxGraphsEmpty;
    private static GameObject _soundsEmpty;
    private static GameObject _projectilesEmpty;
    private static GameObject _trailsEmpty;
    private static GameObject _textsEmpty;


    public static Dictionary<PoolObjects, GameObject> objectDictionary = new Dictionary<PoolObjects, GameObject>();
    public static Dictionary<GameObject, ObjectPool<GameObject>> poolDictionary;
    public static Dictionary<GameObject, GameObject> cloneToPrefabMap;

    public static float defaultObjectLifeTime = 5f;
    void Awake()
    {
        objectDictionary = new Dictionary<PoolObjects, GameObject>();
        cloneToPrefabMap = new Dictionary<GameObject, GameObject>();
        poolDictionary = new Dictionary<GameObject, ObjectPool<GameObject>>();
        CreateObjectDictionary();
        SetupEmpties();

    }

    /*!
     * Creates a dictionary for all objects that can be pooled from a folder in Resources.
     * The Scriptable Object has an PoolObjects enum type, and a GameObject Prefab, which need to be set for every
     * GameObject that is to be pooled.
     * 
     */
    public static void CreateObjectDictionary()
    {
        List<PooledObjectSO> list = new List<PooledObjectSO>();
        foreach (PooledObjectSO data in Resources.LoadAll<PooledObjectSO>(objectsFolderNameInResources))
        {
            Tuple<PoolObjects, GameObject> dataTuple = new Tuple<PoolObjects, GameObject>(data.type, data.gameObjectPF);
            objectDictionary.Add(dataTuple.Item1, dataTuple.Item2);
        }

    }

    /*!
     * Creates empties to hold pooled objects in the hierarchy to keep it nice and clean.
     * When adding more categories they should be added here in the same fashion, and new
     * PoolTypes enums need to be created for them to place the objects where they belong.
     */
    private void SetupEmpties()
    {
        _emptyHolder = new GameObject("---------Object Pools---------");
        _particleSystemsEmpty = new GameObject("Particle Systems");
        _vfxGraphsEmpty = new GameObject("VFX Graphs");
        _gameObjectsEmpty = new GameObject("GameObjects");
        _soundsEmpty = new GameObject("Sound Objects");
        _projectilesEmpty = new GameObject("Projectiles");
        _trailsEmpty = new GameObject("Trails");
        _textsEmpty = new GameObject("Hovering Texts");

        _particleSystemsEmpty.transform.SetParent(_emptyHolder.transform);
        _vfxGraphsEmpty.transform.SetParent(_emptyHolder.transform);
        _gameObjectsEmpty.transform.SetParent(_emptyHolder.transform);
        _soundsEmpty.transform.SetParent(_emptyHolder.transform);
        _projectilesEmpty.transform.SetParent(_emptyHolder.transform);
        _trailsEmpty.transform.SetParent(_emptyHolder.transform);
        _textsEmpty.transform.SetParent(_emptyHolder.transform);
    }



    /*!
     * A method for finding an object based on a PoolObjects type.
     * To create an
     */
    public static GameObject FindObjectWithKey(PoolObjects type)
    {
        if(objectDictionary.TryGetValue(type, out GameObject result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"Could not find object with key: {type}");
            return null;
        }
    }

    /*!
     * Returns object to its Pool. Special care has been placed for Rigidbodies to make sure that their velocity
     * is reset when returning to pool.
     * Can be used like Destroy, but when returning an object from a Pooltypes category, it should be added to return
     * the object to the right place. Otherwise default is GAMEOBJECT.
     */
    public static void ReturnObjectToPool(GameObject obj, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {

        PoolTypes type = SetPoolType(obj);
        if (cloneToPrefabMap.TryGetValue(obj, out GameObject prefab))
        {
            GameObject parentObj = SetParentObject(type);
            if (obj.transform.parent != parentObj.transform)
            {
                obj.transform.SetParent(parentObj.transform);
            }

            if (poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool.Release(obj);
            }
        }
        else
        {
            Debug.LogWarning("Trying to return object that isnt pooled: " + obj.name);
        }
    }

    public static PoolTypes SetPoolType(GameObject obj)
    {
        PoolTypes returnPool;
        if (obj.TryGetComponent(out RigidbodyProjectile proj))
        {

            returnPool = PoolTypes.PROJECTILES;
        }
        else if (obj.TryGetComponent(out ParticleSystem system))
        {

            returnPool = PoolTypes.PARTICLESYSTEMS;
        }
        else if (obj.TryGetComponent(out AudioSource audio))
        {

            returnPool = PoolTypes.SOUNDS;
        }
        else if (obj.TryGetComponent(out TrailRenderer trail))
        {

            returnPool = PoolTypes.TRAILS;
        }
        else if(obj.TryGetComponent(out VisualEffect vfx))
        {
            returnPool = PoolTypes.VFXGRAPHS;
        }
        else if(obj.TryGetComponent(out TextMeshPro text))
        {
            returnPool = PoolTypes.TEXTOBJECTS;
        }
        else
        {
            returnPool = PoolTypes.GAMEOBJECTS;
        }
        return returnPool;
    }
    /*!
     * A method for setting the object parent empty based on its type.
     * If more categories are created they should be added here.
     */
    private static GameObject SetParentObject(PoolTypes pooltype)
    {
        switch (pooltype)
        {
            case PoolTypes.PARTICLESYSTEMS:
                return _particleSystemsEmpty;
            case PoolTypes.GAMEOBJECTS:
                return _gameObjectsEmpty;
            case PoolTypes.VFXGRAPHS:
                return _vfxGraphsEmpty;
            case PoolTypes.SOUNDS:
                return _soundsEmpty;
            case PoolTypes.PROJECTILES:
                return _projectilesEmpty;
            case PoolTypes.TRAILS:
                return _trailsEmpty;
            case PoolTypes.TEXTOBJECTS:
                return _textsEmpty;

            default:
                return null;
        }
        {

        }
    }
    /*!
     * A method to create a pool and its actions. The pool is then added to the pooldictionary for use.
     */
    private static void CreatePool(GameObject prefab, Vector3 pos, Quaternion rot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => CreateObject(prefab, pos, rot, SetPoolType(prefab)),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject
            );
        poolDictionary.Add(prefab, pool);
    }
    /*!
     * Overload method to instead of a location passing just a transform
     */
    private static void CreatePool(GameObject prefab, Transform parent, Quaternion rot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => CreateObject(prefab, parent, rot, SetPoolType(prefab)),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject
            );
        poolDictionary.Add(prefab, pool);
    }

    /*!
     * We create an object and set it to be parented to the empty group it belongs in.
     */
    private static GameObject CreateObject(GameObject prefab, Vector3 pos, Quaternion rot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        prefab.SetActive(false);
        GameObject obj = Instantiate(prefab, pos, rot);
        prefab.SetActive(true);
        GameObject parentObject = SetParentObject(SetPoolType(prefab));
        obj.transform.SetParent(parentObject.transform);

        return obj;
    }

    /*!
     * Overload method for parenting the object to another object instead of to the empty group
     */
    private static GameObject CreateObject(GameObject prefab, Transform parent, Quaternion rot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        prefab.SetActive(false);
        GameObject obj = Instantiate(prefab, parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = rot;
        obj.transform.localScale = Vector3.one;
        prefab.SetActive(true);


        return obj;
    }

    /*!
     * When an object is being pulled from the pool we can do things with it automatically.
     * For now if the object has a particle system component, we play that.
     */
    private static void OnGetObject(GameObject obj)
    {


    }

    /*!
     * When and object is returned to the pool it is set to be inactive to be used again
     */
    private static void OnReleaseObject(GameObject obj)
    {
        obj.SetActive(false);
    }
    /*!
     * When an object is destroyed it needs to be removed from the clone to prefab dictionary
     * 
     */
    private static void OnDestroyObject(GameObject obj)
    {
        if(cloneToPrefabMap.ContainsKey(obj))
        {
            cloneToPrefabMap.Remove(obj);
        }
    }

    /*!
     * Generic Type Constraint Spawn Object function
     * Object is the base class for everything in Unity and thus can be used to spawn anything.
     */
    private static T SpawnObject<T>(GameObject spawnable, Vector3 spawnPos, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS) where T : Object
    {
        if(!poolDictionary.ContainsKey(spawnable))
        {
            CreatePool(spawnable, spawnPos, spawnRot, poolType);
        }

        GameObject obj = poolDictionary[spawnable].Get();

        if(obj != null)
        {
            if(!cloneToPrefabMap.ContainsKey(obj))
            {
                cloneToPrefabMap.Add(obj, spawnable);
            }

            obj.transform.position = spawnPos;
            obj.transform.rotation = spawnRot;
            obj.SetActive(true);

            if(typeof(T) == typeof(GameObject))
            {
                return obj as T;
            }

            T component = obj.GetComponent<T>();

            if(component == null)
            {
                Debug.LogError($"Object {spawnable.name} doesnt have a component of type {typeof(T)}");
                return null;
            }

            return component;
        }
        return null;
    }
    /*!
     * Overload Method for parenting to object
     * 
     */
    private static T SpawnObject<T>(GameObject spawnable, Transform parent, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS) where T : Object
    {
        if (!poolDictionary.ContainsKey(spawnable))
        {
            CreatePool(spawnable, parent, spawnRot, poolType);
        }

        GameObject obj = poolDictionary[spawnable].Get();

        if (obj != null)
        {
            if (!cloneToPrefabMap.ContainsKey(obj))
            {
                cloneToPrefabMap.Add(obj, spawnable);
            }

            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = spawnRot;
            obj.SetActive(true);

            if (typeof(T) == typeof(GameObject))
            {
                return obj as T;
            }

            T component = obj.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError($"Object {spawnable.name} doesnt have a component of type {typeof(T)}");
                return null;
            }

            return component;
        }
        return null;
    }

    /*!
     * All under here are overload methods for item spawning in different situations.
     * First 2 are for spawning generic type and gameobject normally by calling the gameobject or any other Unity object type(Yes, any)
     * Second 2 are for spawning generic type and gameobject but parenting them to another object on spawn like with instantiate.
     * Last 2 are for spawning a Gameobject based on its dictionary key enum, for both normal spawning and parent spawning
     */
    public static T SpawnObject<T>(T prefabType, Vector3 spawnPos, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS) where T : Component
    {
        return SpawnObject<T>(prefabType.gameObject, spawnPos, spawnRot, poolType);
    }

    public static GameObject SpawnObject(GameObject spawnable, Vector3 spawnPos, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        return SpawnObject<GameObject>(spawnable, spawnPos, spawnRot, poolType);
    }

    public static T SpawnObject<T>(T prefabType, Transform parent, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS) where T : Component
    {
        return SpawnObject<T>(prefabType.gameObject, parent, spawnRot, poolType);
    }

    public static GameObject SpawnObject(GameObject spawnable, Transform parent, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        return SpawnObject<GameObject>(spawnable, parent, spawnRot, poolType);
    }
    public static GameObject SpawnObject(PoolObjects dictKey, Vector3 pos, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        return SpawnObject<GameObject>(FindObjectWithKey(dictKey), pos, spawnRot, poolType);
    }

    public static GameObject SpawnObject(PoolObjects dictKey, Transform parent, Quaternion spawnRot, PoolTypes poolType = PoolTypes.GAMEOBJECTS)
    {
        return SpawnObject<GameObject>(FindObjectWithKey(dictKey), parent, spawnRot, poolType);
    }
}
