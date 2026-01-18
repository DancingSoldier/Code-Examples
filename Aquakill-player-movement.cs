using System.Collections;
using UnityEngine;



public class MovementController : MonoBehaviour
{



    private PlayerManager _manager;
    private MovementStateHandler _movementState;
    private CrashingAndDashing _cad;
    [HideInInspector]
    public Rigidbody rb;
    private Transform _groundCheckSphere;
    public LayerMask groundCheckIgnoreMask;
    public Transform orientation;



    /*!
     * Current Input variables. Holds the values to be used in the Update by the Move function. Also used to control dash _direction.
     */
    public float xMovement;
    public float yMovement;

    [Header("Raycasts")]


    [SerializeField, Tooltip("Radius of the ground CheckSphere. Values larger than the players collider allow jumping up walls.")]
    public float groundCheckRadius = .35f;



    [SerializeField, Tooltip("Max angle of a slope the player can move on.")]
    private float _maxSlopeAngle = 45f;
    [SerializeField, Tooltip("Raycast Length. Used in checking when the player is on a slope.")]
    private float _slopeCheckHeight = 1.3f;
    [SerializeField, Tooltip("A downward force modifier. Helps keep the player on the slope when moving upwards to prevent bouncing.")]
    private float _upwardSlopeWeight = 5f;
    private RaycastHit _slopeHit;
    private bool _readyToJump = true;
    private LayerMask _groundLayer;

    // Footstep interval
    private float _nextStepTime;


    private void Awake()
    {
        _manager = GetComponent<PlayerManager>();
        _cad = GetComponent<CrashingAndDashing>();
        _movementState = GetComponent<MovementStateHandler>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        orientation = transform.GetChild(0);
        _groundCheckSphere = transform.GetChild(1).transform;
        _groundLayer = LayerMask.GetMask("Ground");

        
    }

    #region InputActions

    /*!
     * Handles the movement inputs. Called by the Input System
     */
    public void GetMovementInput(Vector2 movementVector)
    {
        xMovement = movementVector.x;
        yMovement = movementVector.y;
    }

    /*!
     * Handles jumping. Called by the Input System.
     */
    public void Jump()
    {
        RBJumping(_manager.jumpForce);
    }
    #endregion

    #region Slopes

    /*!
     * Sends a raycast to check if the player is on a slope.
     */
    public bool OnSlope()
    {
        //! Checks with a raycast if the player is on a slope by comparing its normal to the fired ray to get the angle of the slope.
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, _slopeCheckHeight, _groundLayer))
        {
            //! returns true if the angle of the slope is smaller than the max slope the player can walk on.
            float ang = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return ang < _maxSlopeAngle && ang != 0;
        }
        return false;
    }

    /*!
     * Calculates a movement _direction based of a projected vector that is aligned with the slope the player is on.
     */
    private Vector3 FixSlopeMovement(Vector3 moveDirection)
    {
        //! returns a vector that is parallel to the slope, which can then be used as the new movement vector.
        return Vector3.ProjectOnPlane(moveDirection, _slopeHit.normal).normalized;
    }
    #endregion

    #region Air


    /*!
     * Applies a multiplier to the characters gravity depending on their movement, turning it off when needed.
     */
    private void ApplyGravityChanges()
    {
        //! When in air but not moving vertically, gravity is set to true just in case.
        if(_movementState.currentState == MovementState.AIR && rb.linearVelocity.y == 0)
        {
            rb.useGravity = true;
        }
        //! Rising and not on a slope
        else if (rb.linearVelocity.y > 0 && !OnSlope())
        {
            rb.useGravity = true;
            //! Changes the rising speed.
            rb.linearVelocity += _manager.risingSpeedMultiplier * Physics.gravity.y * Time.deltaTime * Vector3.up;
        }
        //! Falling and not on a slope
        else if (rb.linearVelocity.y < 0 && !OnSlope())
        {
            rb.useGravity = true;
            //! Changes the falling speed.
            rb.linearVelocity += _manager.fallingSpeedMultiplier * Physics.gravity.y * Time.deltaTime * Vector3.up;
        }
        //! When the player is on a slope or is in the DASH state.
        //! Turning the gravity off prevents the player from sliding downwards awkwardly when not moving.
        else if (OnSlope() || _movementState.currentState == MovementState.DASH)
        {
            rb.useGravity = false;

        }

    }

    /*!
     * Handles the drag of the rigidbody on the ground and in the air.
     */
    private void ApplyDrag()
    {
        //! When the player is on the ground.
        if (_movementState.currentState == MovementState.GROUND)
        {
            rb.linearDamping = _manager.groundDrag;
        }
        //! Turn off drag when dashing to make it more predictable.
        else if (_movementState.currentState == MovementState.DASH || _movementState.currentState == MovementState.CRASH)
        {
            rb.linearDamping = 0;
        }
        //! When the player is in the air.
        else
        {
            rb.linearDamping = _manager.airDrag;
        }
    }

    #endregion

    #region General Movement


    /*!
     * Limits the max speed of the Rigidbody's horizontal movement.
     */
    private void ApplySpeedLimits()
    {
        //! Limits the movement speed to a set amount on a slope to prevent te player from moving faster up a slope than on solid ground.
        if(OnSlope())
        {
            if(rb.linearVelocity.magnitude > _manager.moveSpeed)
            {
                rb.linearVelocity =rb.linearVelocity.normalized * _manager.moveSpeed;
            }
        }
        //! Limits the movement speed to a set amount on normal ground.
        else if(_movementState.currentState == MovementState.GROUND)
        {
            Vector3 rawVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (rawVelocity.magnitude > _manager.moveSpeed)
            {
                //! Only applied to x and z so that the falling speed is unaffected.
                Vector3 limitedVelocity = rawVelocity.normalized * _manager.moveSpeed;
                rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);

            }
        }

    }


    /*!
     * Handles movement of the player character.
     * Takes the movement vectors X and Y and applies force according to them to the player.
     */
    private void ApplyRigidBodyMovement(float xMove, float yMove)
    {
        Vector3 movementAmount = (orientation.forward * yMove) + (orientation.right * xMove);
        //! When on a slope the movement vector is fixed by the function.
        if(OnSlope())
        {
            rb.AddForce(10f * _manager.moveSpeed * FixSlopeMovement(movementAmount), ForceMode.Force);

            //! When moving up a slope, a downward force is applied to prevent the player from bouncing or flying upwards.
            if(rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * _upwardSlopeWeight, ForceMode.Force);
            }
        }
        //! When grounded movement is normal.
        else if (_movementState.currentState == MovementState.GROUND)
        {
            rb.AddForce(_manager.moveSpeed * 10f * movementAmount.normalized, ForceMode.Force);
        }
        //! When in the air a modifier is added to change horizontal control in air.
        else if(_movementState.currentState == MovementState.AIR)
        {
            rb.AddForce(_manager.aerialModifier * _manager.moveSpeed * 10f * movementAmount.normalized, ForceMode.Force);
        }
        //! 9.4.25 Added to allow slight movement while dashing
        else if(_movementState.currentState == MovementState.DASH)
        {
            rb.AddForce( _manager.moveSpeed * 5f * movementAmount.normalized, ForceMode.Force);
        }
    }

    /*!
     * Checks if the player can jump, and if they can jump handles the act of jumping.
     * UPDATE: Changed from private to public to allow it to be used elsewhere such as possibly by jump pads.
     */
    public void RBJumping(float jump)
    {
        //! Sets the jumping boolean to true if the player is allowed to perform a jump.

        if (_movementState.currentState == MovementState.GROUND)
        {
            _readyToJump = true;
        }
        //! The Player is not allowed to jump if the condition is not met.
        else
        {
            _readyToJump = false;
        }

        //! If we can jump, we jump!
        if (_readyToJump)
        {
            VFXInstanceManager.Instance.CreateInstantVFX(VFXInstanceManager.Instance.dustImpact, transform.position);
            VFXInstanceManager.Instance.CreateInstantVFX(VFXInstanceManager.Instance.dustImpact, transform.position);
            VFXInstanceManager.Instance.CreateInstantVFX(VFXInstanceManager.Instance.dustImpact, transform.position);
            //! A dampening is applied to limit horizonal velocity when jumping, and y-velocity is returned to 0 to make the jumps always be the same height.
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * _manager.jumpHorizontalDampening, 0f, rb.linearVelocity.z * _manager.jumpHorizontalDampening);
            rb.AddForce(transform.up * jump, ForceMode.Impulse);
            // Play jump sound FX
            SoundFXManager.Instance.PlayPitchedSoundFXClip(_manager.jumpSoundClip, transform, 1f);
        }
    }


    /*!
     * Slows down the players linearvelocity after crashing for a set duration.
     */
    public IEnumerator Brake(float duration)
    {
        float t = 0;
        while (t < duration)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            t += Time.deltaTime;
            yield return null;
        }
    }

    #endregion

    private void PlayFootsteps()
    {
        if (_movementState.currentState == MovementState.GROUND)
        {
            Vector3 rawVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float velocityThreshold = 5.0f;
            if (rawVelocity.magnitude > velocityThreshold && Time.time > _nextStepTime && (xMovement != 0 || yMovement != 0))
            {
                _nextStepTime = Time.time + _manager._currentStepInterval;
                SoundFXManager.Instance.PlayFootstepSounds(_manager.footstepSoundClip, transform, 0.5f);
                if(Random.Range(1, 3) == 1) VFXInstanceManager.Instance.CreateInstantVFX(VFXInstanceManager.Instance.dustImpact, transform.position);

            }
        }
    }

    /*!
     * All functions combined together
     */
    private void UpdateMovement()
    {
        //! What MovementState the player is in.
        _movementState.UpdatePlayerState();

        //! How much drag effects the player.
        ApplyDrag();

        //! Is the player's movement speed limited.
        ApplySpeedLimits();

        //! Is the player effected by gravity.
        ApplyGravityChanges();

        //! Move the player based on inputs and previous factors.
        ApplyRigidBodyMovement(xMovement, yMovement);

        //! Player's footsteps
        PlayFootsteps();
    }

    void FixedUpdate()
    {
        UpdateMovement();
        
    }


}
