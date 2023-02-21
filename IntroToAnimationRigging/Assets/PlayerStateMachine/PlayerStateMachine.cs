using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    // declare reference variables
    CharacterController _characterController;
    Animator _animator;
    PlayerInput _playerInput; // NOTE: PlayerInput class must be generated from New Input System in Inspector

    // variables to store player input values
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _appliedMovement;
    Vector3 _cameraRelativeMovement;
    bool _isMovementPressed;
    bool _isRunPressed;

    // constants
    float _rotationFactorPerFrame = 15.0f;
    float _runMultiplier = 4.0f;
    int _zero = 0;

    // state variables
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    // variables to store optimized setter/getter parameter IDs
    int _isWalkingHash;
    int _isRunningHash;

    // gravity variables
    float _gravity = -9.8f;

    // getters and setters
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; }}
    public Animator Animator { get { return _animator; }}
    public CharacterController CharacterController { get { return _characterController; }}
    public int IsWalkingHash { get { return _isWalkingHash; }}
    public int IsRunningHash { get { return _isRunningHash; }}
    public bool IsMovementPressed { get {return _isMovementPressed; }}
    public bool IsRunPressed { get { return _isRunPressed; }}
    public float Gravity { get { return _gravity; }}
    public float CurrentMovementY { get { return _currentMovement.y; } set { _currentMovement.y = value; } }
    public float AppliedMovementY { get { return _appliedMovement.y; } set { _appliedMovement.y = value; } }
    public float AppliedMovementX { get { return _appliedMovement.x; } set { _appliedMovement.x = value; } }
    public float AppliedMovementZ { get { return _appliedMovement.z; } set { _appliedMovement.z = value; } }
    public float RunMultiplier { get { return _runMultiplier; }}
    public Vector2 CurrentMovementInput { get { return _currentMovementInput; }}
    

    // Awake is called earlier than Start in Unity's event life cycle
    void Awake()
    {
      // initially set reference variables
      _playerInput = new PlayerInput();
      _characterController = GetComponent<CharacterController>();
      _animator = GetComponent<Animator>();

      // setup state
      _states = new PlayerStateFactory(this);
      _currentState = _states.Grounded();
      _currentState.EnterState();

      // set the parameter hash references
      _isWalkingHash = Animator.StringToHash("isWalking");
      _isRunningHash = Animator.StringToHash("isRunning");

      // set the player input callbacks
      _playerInput.CharacterControls.Move.started += OnMovementInput;
      _playerInput.CharacterControls.Move.canceled += OnMovementInput;
      _playerInput.CharacterControls.Move.performed += OnMovementInput;
      _playerInput.CharacterControls.Run.started += OnRun;
      _playerInput.CharacterControls.Run.canceled += OnRun;

    }

    // Start is called before the first frame update
    void Start()
    {
      _characterController.Move(_appliedMovement * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
      HandleRotation();
      _currentState.UpdateStates();
      
      _cameraRelativeMovement = ConvertToCameraSpace(_appliedMovement);
      _characterController.Move(_cameraRelativeMovement * Time.deltaTime);
    }

    Vector3 ConvertToCameraSpace(Vector3 vectorToRotate)
    {
      // store the Y value of the original vector to rotate 
      float currentYValue = vectorToRotate.y;

      // get the forward and right directional vectors of the camera
      Vector3 cameraForward = Camera.main.transform.forward;
      Vector3 cameraRight = Camera.main.transform.right;

      // remove the Y values to ignore upward/downward camera angles
      cameraForward.y = 0;
      cameraRight.y = 0;

      // re-normalize both vectors so they each have a magnitude of 1
      cameraForward = cameraForward.normalized;
      cameraRight = cameraRight.normalized;

      // rotate the X and Z VectorToRotate values to camera space
      Vector3 cameraForwardZProduct = vectorToRotate.z * cameraForward;
      Vector3 cameraRightXProduct = vectorToRotate.x * cameraRight;

      // the sum of both products is the Vector3 in camera space and set Y value
      Vector3 vectorRotatedToCameraSpace = cameraForwardZProduct + cameraRightXProduct;
      vectorRotatedToCameraSpace.y = currentYValue;
      return vectorRotatedToCameraSpace;
    }


    void HandleRotation()
    {
      Vector3 positionToLookAt;
      // the change in position our character should point to
      positionToLookAt.x = _cameraRelativeMovement.x;
      positionToLookAt.y = _zero;
      positionToLookAt.z = _cameraRelativeMovement.z;
      // the current rotation of our character
      Quaternion currentRotation = transform.rotation;

      if (_isMovementPressed)
      {
        // creates a new rotation based on where the player is currently pressing
        Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
        // rotate the character to face the positionToLookAt            
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
      }
    }

    // callback handler function to set the player input values
    void OnMovementInput(InputAction.CallbackContext context)
    {
      _currentMovementInput = context.ReadValue<Vector2>();
      _isMovementPressed = _currentMovementInput.x != _zero || _currentMovementInput.y != _zero;
    }
  
    // callback handler function for run buttons
    void OnRun(InputAction.CallbackContext context)
    {
      _isRunPressed = context.ReadValueAsButton();
    }

    void OnEnable()
    {
      // enable the character controls action map
      _playerInput.CharacterControls.Enable();
    }

    void OnDisable()
    {
      // disable the character controls action map
      _playerInput.CharacterControls.Disable();
    }
}
