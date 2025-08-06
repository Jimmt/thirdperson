using UnityEngine;
using UnityEngine.InputSystem;

public class CursorLockManager : MonoBehaviour {
  [SerializeField] private PlayerInput playerInput;

  void Start() {
    var lockToggleAction = playerInput.actions["Escape"];
    lockToggleAction.performed += context => {
      if (Cursor.lockState == CursorLockMode.Locked) {
        UnlockCursor();
      } else if (Cursor.lockState == CursorLockMode.None) {
        LockCursor();
      }
    };
    Cursor.lockState = CursorLockMode.Locked;
  }

  void OnEnable() => UnlockCursor();
  void OnDisable() => UnlockCursor();

  public void LockCursor() {
    if (enabled) {
      Cursor.lockState = CursorLockMode.Locked;
    }
  }

  private void UnlockCursor() {
    Cursor.lockState = CursorLockMode.None;
  }
}