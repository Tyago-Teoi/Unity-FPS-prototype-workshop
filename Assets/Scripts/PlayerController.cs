using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private CharacterController _characterController;

    [SerializeField] private float _mouseSensibilityFactor = 6f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _airSpeed = 2f;
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -20f;

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;

    private float _x, _y;
    private float _mouseX, _mouseY;

    private Vector3 _move;
    private Vector3 _inertia;
    private float _inertiaCap;

    private PlayerStates _state;

    private Transform _cam;

    private bool _isJumping = false;

    private float _verticalMove;
    private Vector3 _horizontalMove;
    private float _maxAirSpeed = 10f;

    [SerializeField] private float _dashTime = 0.4f;
    [SerializeField] private float _dashForce = 20f;
    private Vector3 _moveBeforDash;
    private bool _isDashing = false;
    private IEnumerator _dashCoroutine;


    void Start()
    {
        _cam = Camera.main.transform;
        _state = PlayerStates.Default;

        InsertPlayerInputActions();
        InsertPlayerActionEvents();
    }

    // Update is called once per frame
    void Update()
    {
        ReadInput();
        DetectAerialState();

        if (_state is PlayerStates.Dash)
        {
            // Movimentar na última direção RAW não zerada do Input de WASD por uma
            // velocidade fixa de 20 unidades por 0.4s e automaticamente reverter
            // para o estado padrão ao terminar

            // Estado Padrão: Se Grounded : Default 
            // Caso contrário: Descend            
            Dash();
        }

        if (_state is PlayerStates.Ascend ||  _state is PlayerStates.Descend)
        {
            GetHorizontalAndVertical();

            _inertia += _horizontalMove * _airSpeed * Time.deltaTime;
            _inertia = Vector3.ClampMagnitude(_inertia, _inertiaCap);

            _move = _inertia;
            _move.y = _verticalMove;
        }

        if (_state is PlayerStates.Default)
        {
            Move(_speed);
        }

        _move += Vector3.up * _gravity * Time.deltaTime;

        _characterController.Move(_move * Time.deltaTime);

        if (_characterController.isGrounded)
            _move.y = -5;

        _cam.position = this.transform.position + Vector3.up * 1f;

        this.transform.rotation = Quaternion.Euler(new Vector3(0, _mouseX, 0));
    }

    private void LateUpdate()
    {
        _cam.rotation = Quaternion.Euler(new Vector3(-_mouseY, _mouseX, 0));
    }

    private void InsertPlayerInputActions()
    {
        _moveAction = _playerInput.actions["Move"];
        _lookAction = _playerInput.actions["Look"];
        _jumpAction = _playerInput.actions["Jump"];
        _dashAction = _playerInput.actions["Dash"];
    }

    private void InsertPlayerActionEvents()
    {
        _jumpAction.performed += _jumpAction_performed;
        _dashAction.performed += _dashAction_performed;
    }

    private void ReadInput()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        _x = moveInput.x;
        _y = moveInput.y;

        Vector2 mouse = _lookAction.ReadValue<Vector2>() * _mouseSensibilityFactor;
        _mouseX += mouse.x;
        _mouseY += mouse.y;

        _mouseY = Mathf.Clamp(_mouseY, -75, 75);
    }

    void DetectAerialState()
    {
        if (_isDashing)
        {
            _state = PlayerStates.Dash;
            return;
        }
        if (_characterController.isGrounded)
        {
            _state = PlayerStates.Default;
            return;
        }
        if (_move.y > 0f)
        {
            _state = PlayerStates.Ascend;
            return;
        }
        if (_move.y < -2f)
            _state = PlayerStates.Descend;
    }   

    private void Move(float speed)
    {
        GetHorizontalAndVertical();

        _horizontalMove = this.transform.rotation * _horizontalMove * speed;

        _move = _horizontalMove;
        _move.y = _verticalMove;
    }
    private void Dash()
    {
        GetHorizontalAndVertical();

        _horizontalMove = this.transform.rotation * Vector3.forward * _dashForce;
        _move = _horizontalMove;
        _move.y = 0;
    }

    private void GetHorizontalAndVertical()
    {
        _verticalMove = _move.y;
        _horizontalMove = new Vector3(_x, 0, _y);
    }


    // -- Events
    private void _jumpAction_performed(InputAction.CallbackContext obj)
    {
        if (_characterController.isGrounded) 
        {
            _inertia = new Vector3(_move.x, 0, _move.z);
            _inertiaCap = Mathf.Clamp(_inertia.magnitude, _maxAirSpeed, 99);
            //_inertiaCap = _inertia.magnitude;

            _move = this.transform.position + Vector3.up * _jumpForce;
        }
    }

    private void _dashAction_performed(InputAction.CallbackContext context)
    {
        _dashCoroutine = StartDashAndWait();
        StartCoroutine(_dashCoroutine);
    }

    private IEnumerator StartDashAndWait()
    {
        if (_isDashing)
            yield break;

        _moveBeforDash = _move;
        _isDashing = true;

        yield return new WaitForSeconds(_dashTime);

        _move = _moveBeforDash;
        _isDashing = false;
    }
}