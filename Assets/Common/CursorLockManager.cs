using UnityEngine;
using UnityEngine.InputSystem;

public class CursorLockManager : MonoBehaviour {
  [SerializeField] private PlayerInput playerInput;

  private InputAction lockToggleAction;

  void Start() {
    lockToggleAction = playerInput.actions["Escape"];
    lockToggleAction.performed += ToggleLock;
    Cursor.lockState = CursorLockMode.Locked;
  }

  void OnEnable() => UnlockCursor();

  void OnDisable() {
    UnlockCursor();
    lockToggleAction.performed -= ToggleLock;
  }

  private void ToggleLock(InputAction.CallbackContext context) {
    if (Cursor.lockState == CursorLockMode.Locked) {
      UnlockCursor();
    } else if (Cursor.lockState == CursorLockMode.None) {
      LockCursor();
    }
  }

  private void LockCursor() {
    if (enabled) {
      Cursor.lockState = CursorLockMode.Locked;
    }
  }

  private void UnlockCursor() {
    Cursor.lockState = CursorLockMode.None;
  }
}