using Common;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
  [SerializeField] private float moveSpeed = 5;
  [SerializeField] private float mouseSensitivity = 15f;
  [SerializeField] private float aimingSensitivityRatio = 0.75f;
  [SerializeField] private GameObject followTarget;
  [SerializeField] private GameObject mainCamera;
  [SerializeField] private GameObject aimCamera;

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

  void Start() {
    charController = GetComponent<CharacterController>();
    PlayerInput playerInput = GetComponent<PlayerInput>();
    moveAction = playerInput.actions["Move"];
    shootAction = playerInput.actions["Shoot"];
    altFireAction = playerInput.actions["AlternateFire"];
    lookAction = playerInput.actions["Look"];

    EventController.TriggerAimingStateChanged(isAiming);
    shootAction.performed += context => Shoot();
    altFireAction.performed += context => {
      mainCamera.SetActive(false);
      aimCamera.SetActive(true);

      isAiming = true;
      EventController.TriggerAimingStateChanged(isAiming);
    };
    altFireAction.canceled += context => {
      mainCamera.SetActive(true);
      aimCamera.SetActive(false);

      isAiming = false;
      EventController.TriggerAimingStateChanged(isAiming);
    };
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
  }

  void Shoot() {
    // raycast etc
  }
}