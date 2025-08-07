using System;
using Common;
using EzySlice;
using UnityEngine;
using UnityEngine.InputSystem;
using Plane = UnityEngine.Plane;

public class Player : MonoBehaviour {
  [SerializeField] private float moveSpeed = 5;
  [SerializeField] private float mouseSensitivity = 15f;
  [SerializeField] private float aimingSensitivityRatio = 0.75f;
  [SerializeField] private GameObject followTarget;
  [SerializeField] private GameObject mainCamera;
  [SerializeField] private GameObject aimCamera;

  // todo this should not be a hard reference
  [SerializeField] private GameObject enemy;

  private CharacterController charController;
  private InputAction moveAction;
  private InputAction shootAction;
  private InputAction altFireAction;
  private InputAction lookAction;

  private float verticalVelocity = 0f;
  private float horizontalRotation = 0f;
  private float verticalRotation = 0f;

  // TODO: state machine
  private bool isAiming = false;
  private Vector3 sliceStart;
  private Vector3 sliceStartDir;
  private Vector3 sliceEnd;
  private Vector3 sliceEndDir;

  private void Awake() {
    PlayerInput playerInput = GetComponent<PlayerInput>();
    moveAction = playerInput.actions["Move"];
    shootAction = playerInput.actions["Shoot"];
    altFireAction = playerInput.actions["AlternateFire"];
    lookAction = playerInput.actions["Look"];
  }

  void Start() {
    charController = GetComponent<CharacterController>();
    EventController.TriggerAimingStateChanged(isAiming);
  }

  private void OnEnable() {
    Debug.Log("OnEnable");
    shootAction.performed += ShootDown;
    shootAction.canceled += ShootRelease;
    altFireAction.performed += AltFireDown;
    altFireAction.canceled += AltFireRelease;
  }

  private void OnDisable() {
    Debug.Log("OnDisable");
    shootAction.performed -= ShootDown;
    shootAction.canceled -= ShootRelease;
    altFireAction.performed -= AltFireDown;
    altFireAction.canceled -= AltFireRelease;
  }

  void Update() {
    float sens = mouseSensitivity * (isAiming ? aimingSensitivityRatio : 1f);
    Vector2 look = lookAction.ReadValue<Vector2>() * (sens * Time.deltaTime);
    // todo some jitters
    horizontalRotation += look.x;
    verticalRotation -= look.y;
    transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    followTarget.transform.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);

    if (charController.isGrounded) {
      if (verticalVelocity < 0) verticalVelocity = 0f;
    }

    verticalVelocity += Physics.gravity.y * Time.deltaTime;

    Vector2 moveInput = moveAction.ReadValue<Vector2>().normalized * moveSpeed;
    // Interpret input as local coords, since it should be relative to player/camera facing. 
    Vector3 moveWorld = transform.TransformDirection(new Vector3(moveInput.x, verticalVelocity, moveInput.y));
    charController.Move(moveWorld * Time.deltaTime);

    // --- DEBUG ---
    Debug.DrawRay(lastDebugPos, sliceStartDir, Color.green);
    Debug.DrawRay(lastDebugPos, sliceEndDir, Color.red);
    Debug.DrawRay(lastDebugPos, planeNormal, Color.yellow);
  }

  void AltFireDown(InputAction.CallbackContext context) {
    mainCamera.SetActive(false);
    aimCamera.SetActive(true);

    isAiming = true;
    EventController.TriggerAimingStateChanged(isAiming);
  }

  void AltFireRelease(InputAction.CallbackContext context) {
    mainCamera.SetActive(true);
    aimCamera.SetActive(false);

    isAiming = false;
    EventController.TriggerAimingStateChanged(isAiming);
  }

  void ShootDown(InputAction.CallbackContext context) {
    if (!isAiming) return;
    var mainCam = Camera.main;
    if (mainCam == null) return;
    
    Vector3 pos = aimCamera.transform.position;
    sliceStart = mainCam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 7f));
    sliceStartDir = sliceStart - pos;
  }

  private Vector3 planeNormal;
  private Vector3 lastDebugPos;

  void ShootRelease(InputAction.CallbackContext context) {
    if (!isAiming) return;
    var mainCam = Camera.main;
    if (mainCam == null) return;
    
    Vector3 pos = mainCam.transform.position;
    lastDebugPos = pos;
    sliceEnd = mainCam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 7f));
    sliceEndDir = sliceEnd - pos;

    planeNormal = Vector3.Cross(sliceStartDir, sliceEndDir).normalized;
    Vector3 pointOnPlane = pos;
    Plane slicePlane = new Plane(planeNormal, pointOnPlane);
    
    GameObject[] enemyParts = enemy.SliceInstantiate(pointOnPlane, planeNormal);
    if (enemyParts == null) {
      Debug.LogWarning("Empty parts for slice");
      return;
    }
    enemy.SetActive(false);
    for (int i = 0; i < enemyParts.Length; i++) {
      var slice = enemyParts[i];
      slice.AddComponent<MeshCollider>().convex = true;
      var rb = slice.AddComponent<Rigidbody>();
      
      Vector3 frontForce = (rb.position - mainCamera.transform.position) * 0.5f;
      Vector3 force = frontForce;
      float verticalScalar = 2f;
      // var isPositiveSide = slicePlane.GetSide(rb.position); rb.position is identical for parts for some reason (takes a frame?)
      if (i == 0) { // Above plane
        force += planeNormal * verticalScalar;
      } else if (i == 1) { // Below plane
        force = -planeNormal * verticalScalar;
      }
      Debug.Log((i == 0) + " part_position=" + rb.position + " normal=" + planeNormal);
      Debug.Log("[" + i + "] = " + force);
      
      rb.AddForce(force, ForceMode.Impulse);
    }
  }
}