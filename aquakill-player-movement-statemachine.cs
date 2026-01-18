
using UnityEngine;

public class MovementStateHandler : MonoBehaviour
{

    /* 
 *             The Movement State Handler Script
 *          
 *                       An enum Poem
 *      
 *        If State is not GROUND, then it has to be AIR,
 *          except if the player has started to CRASH.
 *             If it was CRASH and is now GROUND,
 *         the player has just crashed to the ground.
 *                      Make a splash.
 *       
 *       
 *                 Unless it was always DASH.
 *       
 *       
 *       - Joonatan 28.03.2025 1.23 AM
 *     
 * 
 *          ------------How it works------------
 *          
 *          
 * The player can be in different MovementStates which are listed here.
 *  
 *                    The States are:
 * 
 * - GROUND     player is on the ground.                                        -----> can DASH but can not CRASH
 * - AIR        player is in the air but is not crashing.                       -----> Can DASH or CRASH if they're high enough in the air
 * - CRASH      player has pressed the input for crashing.                      -----> Cannot DASH
 * - DASH       player has pressed the input for dashing.                       -----> Can CRASH
 * 
 * 
 *  Depending on the state the player is in, their movement is effected in different ways.
 *  
 *              The states are changed based on:
 * 
 *  - What is the layer of the Game Object the player is standing on.
 *  - Inputs the player has given (Dash or Crash)
 *  
 *  Crashing and Dashing functions are placed on their own script file.
 *  The other Player Control scripts ask what state the player is in currently, or had been previously, and change the forces effecting the rigidbody based on that.
 * 
 * 
 * 
 */

    private MovementController _movement;
    private PlayerManager _manager;
    private CrashingAndDashing _cad;
    [SerializeField]
    private Transform _groundCheckSphere;

    public MovementState currentState;

    //! Used to check when the player has crashed to a layer it can crash on.
    public MovementState crashCheckState;

    private void Start()
    {
        _movement = transform.GetComponent<MovementController>();
        _manager = transform.GetComponent<PlayerManager>();
        _groundCheckSphere = transform.GetChild(1).transform;
        _cad = transform.GetComponent<CrashingAndDashing>();
    }


    /*!
     * Checks what layer the player is on and returns an integer based on that.
     */
    public int LayerCheck()
    {
        /*
         * Note: This could be used to check when the player has walked over a pickup without needing to do anything else with triggers or colliders.
         */

        //! An Overlap Sphere located at the feet of the player returns an array of the colliders hit. The "Player" layer is ignored.
        //! Edit: 07.04 added Projectile layer to the mask to preent projectiles from cancelling crash mid air when shooting down.
        Collider[] hitLayers = Physics.OverlapSphere(_groundCheckSphere.position, _movement.groundCheckRadius, ~_movement.groundCheckIgnoreMask);

        //! If the length of the returned array is not 0, the player is standing on a surface.
        if (hitLayers.Length != 0)
        {
            //! The first index of the layer is deemed the surface that the player is standing on.
            LayerMask hitLayer = hitLayers[0].gameObject.layer;
            //! The switch statement then returns the index of the layer currently stood over.
            switch (hitLayer)
            {
                //! Default 
                case 0:
                    //! ground
                    return 6;

                //! Ground special
                case 6:
                    return 6;
                //! Weapon fall through 
                case 7:
                    return 0;
                //! Jump Pad special
                case 8:
                    return 8;
                //! Player ground
                case 9:
                    return 6;
                //! Enemy
                case 10:
                    return 10;
                //! Projectile ground
                case 11:
                    return 6;
                //! Waypoint fall through 
                case 12:
                    return 0;
                //! Hidden fall through
                case 13:
                    return 0;
                //! Pickup fall through
                case 14:
                    return 0;
                case 15:
                    return 6;

                case 16:
                    return 0;
                case 17:
                    return 0;
                //! When more layers are added that effect the player when stood over, they should be added here.
                default:
                    return 6;

            }
        }
        //! When a 0 is returned, the player is concidered to not stand on anything.
        else return 0;
    }
    public void UpdatePlayerState()
    {

        /*! If the player is dashing, no ground checks are made as dashing overrules all other MovementStates because it can be done anywhere.
         *  When the dash is finished or cancelled by a crash, the state is set to either CRASH or GROUND by the coroutine DashState in CrashingAndDashing.
         */
        if (CheckDashState()) return;

        //! LayerIndex is the index of the layer that the object the player is standing on has.
        int layerIndex = LayerCheck();

        /*!
         * In AIR (Layers nothing and Pickup)
         */
        if (layerIndex == 0 || layerIndex == 14)
        {
            CheckAirState();
        }

        if (layerIndex == 8)
        {

            currentState = MovementState.AIR;
        }
        //! On GROUND. (Layer Ground or Default)
        else if (layerIndex == 6)
        {
            _cad.CanBeCrashedOn();   //!< Checks if the player has crashed when landing on the layer. If this is not called the layer cannot be crashed on.
            ResetStates();
        }
        //! Enemy Layer  (Layer Enemy)
        else if (layerIndex == 10)
        {
            Debug.Log("You've landed on an Enemy!");
            _cad.CanBeCrashedOn();
            ResetStates();
        }
        //Debug.Log(LayerCheck());
    }

    /*!
     * Checks if the Player is currently dashing.
     */
    private bool CheckDashState()
    {
        if (currentState == MovementState.DASH)
        {
            return true;
        }
        return false;
    }
    /*!
     * Sets the state to AIR if the player is not crashing but is in the air.
     */
    private void CheckAirState()
    {
        if (currentState != MovementState.CRASH)
        {
            currentState = MovementState.AIR;

        }
    }
    /*!
     * Resets the current state and the state used in crash checking.
     */
    private void ResetStates()
    {
        currentState = MovementState.GROUND;
        crashCheckState = MovementState.GROUND;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 pos = _groundCheckSphere.position;
        Vector3 player = transform.position;
        CapsuleCollider[] colliderRad = transform.GetComponents<CapsuleCollider>();
        float rad0 = colliderRad[0].radius;
        float rad = colliderRad[1].radius;
        float radius = .35f;
        Gizmos.DrawWireSphere(pos, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player, rad);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player, rad0);

    }

}

