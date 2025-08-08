using System.Collections;
using System.Collections.Generic;
using Common;
using EzySlice;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Player : MonoBehaviour {
  [SerializeField] private float moveSpeed = 5;
  [SerializeField] private float mouseSensitivity = 15f;
  [SerializeField] private float aimingSensitivityRatio = 0.75f;
  [SerializeField] private GameObject followTarget;
  [SerializeField] private GameObject gun;
  [SerializeField] private GameObject mainCamera;
  [SerializeField] private GameObject aimCamera;
  [SerializeField] private GameObject trail;
  [SerializeField] private GameObject physicsSliceBox;

  // todo should not depend directly on enemy
  [SerializeField] private GameObject enemy;

  [SerializeField] private Material enemyMaterial;

  // todo should not maintain this here
  private List<GameObject> enemies = new List<GameObject>();

  private CharacterController charController;
  private InputAction moveAction;
  private InputAction shootAction;
  private InputAction altFireAction;
  private InputAction lookAction;
  private InputAction switchWeaponAction;
  private SlashEffectGenerator slashEffectGenerator;

  private float verticalVelocity = 0f;
  private float horizontalRotation = 0f;
  private float verticalRotation = 0f;

  // TODO: state machine
  enum Weapon {
    Gun,
    Knife
  }

  private Weapon activeWeapon = Weapon.Gun;
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
    switchWeaponAction = playerInput.actions["SwitchWeapon"];
    slashEffectGenerator = GetComponent<SlashEffectGenerator>();
  }

  void Start() {
    charController = GetComponent<CharacterController>();
    EventController.TriggerAimingStateChanged(isAiming);
    EventController.TriggerWeaponSelected(activeWeapon.ToString());
    enemies.Add(enemy);
  }

  private void OnEnable() {
    Debug.Log("OnEnable");
    shootAction.performed += ShootDown;
    shootAction.canceled += ShootRelease;
    altFireAction.performed += AltFireDown;
    altFireAction.canceled += AltFireRelease;
    switchWeaponAction.performed += SwitchWeapon;
  }

  private void OnDisable() {
    Debug.Log("OnDisable");
    shootAction.performed -= ShootDown;
    shootAction.canceled -= ShootRelease;
    altFireAction.performed -= AltFireDown;
    altFireAction.canceled -= AltFireRelease;
    switchWeaponAction.performed -= SwitchWeapon;
  }

  void Update() {
    float sens = mouseSensitivity * (isAiming ? aimingSensitivityRatio : 1f);
    Vector2 look = lookAction.ReadValue<Vector2>() * (sens * Time.deltaTime);
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

  void SwitchWeapon(InputAction.CallbackContext context) {
    SwitchWeapon();
  }

  void SwitchWeapon() {
    if (activeWeapon == Weapon.Gun) {
      activeWeapon = Weapon.Knife;
    } else {
      activeWeapon = Weapon.Gun;
    }

    EventController.TriggerWeaponSelected(activeWeapon.ToString());
  }

  void ShootDown(InputAction.CallbackContext context) {
    if (activeWeapon == Weapon.Knife) {
      KnifeDown();
    } else {
      GunDown();
    }
  }

  private Vector3 planeNormal;
  private Vector3 lastDebugPos;

  void ShootRelease(InputAction.CallbackContext context) {
    if (activeWeapon == Weapon.Knife) {
      KnifeRelease();
    }
  }

  void GunDown() {
    if (!isAiming) return;
    var mainCam = Camera.main;
    if (mainCam == null) return;

    RaycastHit hit;
    float maxDist = 40f;
    var raycastHit = Physics.Raycast(mainCam.transform.position, mainCam.transform.forward, out hit, maxDist);
    Debug.Log(raycastHit);
    Vector3 endPosition = raycastHit ? hit.point : (mainCam.transform.position + mainCam.transform.forward * maxDist);
    TrailRenderer trailRenderer =
      Instantiate(trail, gun.transform.position, Quaternion.identity).GetComponent<TrailRenderer>();
    StartCoroutine(SpawnTrail(trailRenderer, endPosition));
  }

  private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 endPosition) {
    float time = 0;
    Vector3 startPos = trail.transform.position;

    while (time < 1) {
      trail.transform.position = Vector3.Lerp(startPos, endPosition, time);
      time += Time.deltaTime / trail.time;
      yield return null;
    }

    trail.transform.position = endPosition;
    Destroy(trail.gameObject, 0.1f);
    // todo spawn impact effects
  }

  void KnifeDown() {
    if (!isAiming) return;
    var mainCam = Camera.main;
    if (mainCam == null) return;

    Vector3 pos = aimCamera.transform.position;
    sliceStart = mainCam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 7f));
    sliceStartDir = sliceStart - pos;
  }

  void KnifeRelease() {
    if (!isAiming) return;
    var mainCam = Camera.main;
    if (mainCam == null) return;

    Vector3 pos = mainCam.transform.position;
    lastDebugPos = pos;
    sliceEnd = mainCam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 7f));
    sliceEndDir = sliceEnd - pos;

    planeNormal = Vector3.Cross(sliceStartDir, sliceEndDir).normalized;
    Vector3 pointOnPlane = pos;

    Vector3 sliceMiddle = (sliceEnd + sliceStart) / 2;
    Vector3 slashEffectStart = mainCam.transform.position;
    Vector3 slashTilt = Vector3.Cross(mainCam.transform.position, sliceEnd - sliceStart);
    slashEffectGenerator.SpawnSlash(slashEffectStart, Quaternion.LookRotation(mainCam.transform.forward, slashTilt),
      sliceMiddle - slashEffectStart);

    var enemiesCopy = new List<GameObject>(enemies);
    foreach (var e in enemiesCopy) {
      Slice(e, pointOnPlane, planeNormal);
    }
  }

  private void Slice(GameObject enemyToSlice, Vector3 pointOnPlane, Vector3 planeNormal) {
    GameObject[] enemyParts = enemyToSlice.SliceInstantiate(pointOnPlane, planeNormal, enemyMaterial);
    if (enemyParts == null) {
      Debug.LogWarning("Empty parts for slice");
      return;
    }

    // enemy.SetActive(false);
    enemies.Remove(enemyToSlice);
    Destroy(enemyToSlice);
    for (int i = 0; i < enemyParts.Length; i++) {
      // for further sub-slicing of parts
      var part = enemyParts[i];
      enemies.Add(part);
      part.layer = 7; // [Enemy] todo use a constant
      part.AddComponent<MeshCollider>().convex = true;
      var rb = part.AddComponent<Rigidbody>();

      Vector3 frontForce = (rb.position - mainCamera.transform.position) * 0.5f;
      Vector3 force = frontForce;
      float verticalScalar = 2f;
      // var isPositiveSide = slicePlane.GetSide(rb.position); rb.position is identical for parts for some reason (takes a frame?)
      if (i == 0) {
        // Above plane
        force += planeNormal * verticalScalar;
      } else if (i == 1) {
        // Below plane
        force = -planeNormal * verticalScalar;
      }

      Debug.Log((i == 0) + " part_position=" + rb.position + " normal=" + planeNormal);
      Debug.Log("[" + i + "] = " + force);

      rb.AddForce(force, ForceMode.Impulse);
    }
  }
}