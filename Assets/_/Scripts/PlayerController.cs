using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Pathfinding;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.IO;
using UnityEngine.InputSystem.XR;
using Unity.PlasticSCM.Editor.WebApi;

public struct MoveData
{
    public Vector2? Target;
}

public struct ReconcileData
{
    public bool IsMoving;
    public Vector2 Position;
}

public class PlayerController : NetworkBehaviour
{

    [SerializeField]
    private const float _playerY = 0.5f;
    [SerializeField]
    private const float _dist = 0.15f;
    [SerializeField]
    private float _moveSpeed = 5f;
    [SerializeField]
    private LayerMask _layerMask;

    [SerializeField]
    private float _cameraYOffset = 10f;
    [SerializeField]
    private float _zoomRate = 2f;
    [SerializeField]
    private float _panRate = 10f;
    private Camera _camera;

    private Seeker _seeker;

    private PlayerControl _playerControl;
    private bool _isCameraLocked = true;

    private Pathfinding.Path _path;

    private float _nextWaypointDistance = 3f;

    private int _currentWaypoint = 0;

    private bool _reachedEndOfPath;

    private Vector2? _moveTarget;

    private int _team = 0;

    //Cooldowns
    private float _a1cd = 1.0f;
    private float _a2cd = 1.0f;
    private float _a3cd = 1.0f;
    private float _a4cd = 1.0f;
    private float _s1cd = 1.0f;
    private float _s2cd = 1.0f;


    private void Awake()
    {
        _seeker = GetComponent<Seeker>();

        _playerControl = new PlayerControl();
    }

    #region Input Events

    private void OnToggleCameraLock(InputAction.CallbackContext obj)
    {
        _isCameraLocked ^= true;
    }

    private void OnFocusPlayer(InputAction.CallbackContext obj)
    {
        _camera.transform.position = new Vector3(transform.position.x, transform.position.y + _cameraYOffset, transform.position.z);
    }

    private void OnZoomCamera(InputAction.CallbackContext obj)
    {
        float mouseScrollY = _playerControl.Main.Zoom.ReadValue<float>();

        if (mouseScrollY > 0)
            _camera.fieldOfView += _zoomRate;
        else if (mouseScrollY < 0)
            _camera.fieldOfView -= _zoomRate;
    }

    private void OnRMBAction(InputAction.CallbackContext obj)
    {
        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, _layerMask))
            _moveTarget = new Vector2(hit.point.x, hit.point.z);
    }

    private void OnStop(InputAction.CallbackContext obj)
    {
        _moveTarget = new Vector2(transform.position.x, transform.position.z);
    }

    private void OnAbility1(InputAction.CallbackContext obj)
    {
        Debug.Log("Ability 1");
    }

    private void OnAbility2(InputAction.CallbackContext obj)
    {
        Debug.Log("Ability 2");
    }
    private void OnAbility3(InputAction.CallbackContext obj)
    {
        Debug.Log("Ability 3");
    }

    private void OnAbility4(InputAction.CallbackContext obj)
    {
        Debug.Log("Ability 4");
    }

    private void OnSpell1(InputAction.CallbackContext obj)
    {
        Debug.Log("Spell 1");
    }

    private void OnSpell2(InputAction.CallbackContext obj)
    {
        Debug.Log("Spell 2");
    }

    #endregion

    #region Start / Stop

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            _camera = Camera.main;
            _playerControl.Main.ToggleCameraLock.performed += OnToggleCameraLock;
            _playerControl.Main.ToggleCameraLock.Enable();

            _playerControl.Main.FocusPlayer.performed += OnFocusPlayer;
            _playerControl.Main.FocusPlayer.Enable();

            _playerControl.Main.Zoom.performed += OnZoomCamera;
            _playerControl.Main.Zoom.Enable();

            _playerControl.Main.RMBAction.performed += OnRMBAction;
            _playerControl.Main.RMBAction.Enable();

            _playerControl.Main.Stop.performed += OnStop;
            _playerControl.Main.Stop.Enable();

            _playerControl.Main.Ability1.performed += OnAbility1;
            _playerControl.Main.Ability1.Enable();

            _playerControl.Main.Ability2.performed += OnAbility2;
            _playerControl.Main.Ability2.Enable();

            _playerControl.Main.Ability3.performed += OnAbility3;
            _playerControl.Main.Ability3.Enable();

            _playerControl.Main.Ability4.performed += OnAbility4;
            _playerControl.Main.Ability4.Enable();

            _playerControl.Main.Spell1.performed += OnSpell1;
            _playerControl.Main.Spell1.Enable();

            _playerControl.Main.Spell2.performed += OnSpell2;
            _playerControl.Main.Spell2.Enable();
        } 
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (base.IsOwner)
        {
            _playerControl.Main.ToggleCameraLock.performed -= OnToggleCameraLock;
            _playerControl.Main.ToggleCameraLock.Disable();

            _playerControl.Main.FocusPlayer.performed -= OnFocusPlayer;
            _playerControl.Main.FocusPlayer.Disable();

            _playerControl.Main.Zoom.performed -= OnZoomCamera;
            _playerControl.Main.Zoom.Disable();

            _playerControl.Main.RMBAction.performed -= OnRMBAction;
            _playerControl.Main.RMBAction.Disable();

            _playerControl.Main.Stop.performed -= OnStop;
            _playerControl.Main.Stop.Disable();

            _playerControl.Main.Ability1.performed -= OnAbility1;
            _playerControl.Main.Ability1.Disable();

            _playerControl.Main.Ability2.performed -= OnAbility2;
            _playerControl.Main.Ability2.Disable();

            _playerControl.Main.Ability3.performed -= OnAbility3;
            _playerControl.Main.Ability3.Disable();

            _playerControl.Main.Ability4.performed -= OnAbility4;
            _playerControl.Main.Ability4.Disable();

            _playerControl.Main.Spell1.performed -= OnSpell1;
            _playerControl.Main.Spell1.Disable();

            _playerControl.Main.Spell2.performed -= OnSpell2;
            _playerControl.Main.Spell2.Disable();
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (base.IsServer || base.IsClient)
            base.TimeManager.OnTick += TimeManager_OnTick;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (base.TimeManager != null)
            base.TimeManager.OnTick -= TimeManager_OnTick;
    }

    #endregion

    public void OnDisable()
    {
        _seeker.pathCallback -= OnPathComplete;
    }

    private void Update()
    {
        if (!base.IsOwner)
            return;
        if (_isCameraLocked)
            _camera.transform.position = new Vector3(transform.position.x, transform.position.y + _cameraYOffset, transform.position.z);
        else
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (mousePos.x <= 0)
                _camera.transform.Translate(new Vector3(Time.deltaTime * _panRate * -1f, 0f, 0f), Space.World);
            else if(mousePos.x >= Screen.width)
                _camera.transform.Translate(new Vector3(Time.deltaTime * _panRate, 0f, 0f), Space.World);
            if (mousePos.y <= 0)
                _camera.transform.Translate(new Vector3(0f, 0f, Time.deltaTime * _panRate * -1f), Space.World);
            else if (mousePos.y >= Screen.height)
                _camera.transform.Translate(new Vector3(0f, 0f, Time.deltaTime * _panRate), Space.World);
        }
    }

    /// <summary>
    /// Called every time the TimeManager ticks.
    /// This will occur at your TickDelta, generated from the configured TickRate.
    /// </summary>
    private void TimeManager_OnTick()
    {
        if (base.IsOwner)
        {
            Reconcile(default, false);
            BuildActions(out MoveData md);
            Move(md, false);
        }
        if (base.IsServer)
        {
            Move(default, true);
            ReconcileData rd = new ReconcileData()
            {
                IsMoving = _path != null,
                Position = new Vector2(transform.position.x, transform.position.z)
            };
            Reconcile(rd, true);
        }
    }

    private void BuildActions(out MoveData moveData)
    {
        moveData = default;
        moveData.Target = _moveTarget;

        //Unset queued values.
        _moveTarget = null;
    }

    [Replicate]
    private void Move(MoveData moveData, bool asServer, bool replaying = false)
    {
        if (moveData.Target != null)
        {
            Vector2 targ = moveData.Target.GetValueOrDefault();
            Debug.Log(targ);
            _reachedEndOfPath = false;
            _seeker.StartPath(transform.position, new Vector3(targ.x, _playerY, targ.y), OnPathComplete);

        }

        if (_path != null)
        {
            if (_reachedEndOfPath == false)
            {
                float distanceToWaypoint;
                while (true)
                {
                    distanceToWaypoint = Vector3.Distance(transform.position, _path.vectorPath[_currentWaypoint]);
                    if (distanceToWaypoint < _dist/*_nextWaypointDistance*/)
                    {
                        if (_currentWaypoint + 1 < _path.vectorPath.Count)
                        {
                            _currentWaypoint++;
                            transform.rotation = Quaternion.LookRotation(_path.vectorPath[_currentWaypoint] - transform.position);
                            Debug.Log("move rotation set");
                        }
                        else
                        {
                            Debug.Log("last waypoint");
                            _reachedEndOfPath = true;
                            break;
                        }
                    }
                    else
                        break;
                }
            }

            Vector3 dir = (_path.vectorPath[_currentWaypoint] - transform.position).normalized;
            Vector3 velocity = dir * _moveSpeed;

            transform.position += velocity * (float)base.TimeManager.TickDelta;

            Vector2 targ = moveData.Target.GetValueOrDefault();
            //Debug.Log(Vector3.Distance(transform.position, _path.vectorPath[_currentWaypoint]));
            if (_reachedEndOfPath && Vector3.Distance(transform.position, _path.vectorPath[_currentWaypoint]) <= _dist)
            {
                transform.position = _path.vectorPath[_currentWaypoint];
                _path = null;
                Debug.Log("path end");
            }
        }
    }

    private void OnPathComplete(Pathfinding.Path p)
    {
        if (!p.error)
        {
            _path = p;
            _currentWaypoint = 0;
            if (_path.vectorPath.Count > 0)
            {
                transform.rotation = Quaternion.LookRotation(_path.vectorPath[_currentWaypoint] - transform.position);
                Debug.Log("opc rotation set");
            }
        }
    }

    /// <summary>
    /// Resets the client to ReconcileData.
    /// </summary>
    [Reconcile]
    private void Reconcile(ReconcileData recData, bool asServer)
    {
        /* Reset the client to the received position. It's okay to do this
         * even if there is no de-synchronization. */
        if(!recData.IsMoving)
        {
            _reachedEndOfPath = true;
            _path = null;
        }
        transform.position = new Vector3(recData.Position.x, _playerY, recData.Position.y);
    }

}