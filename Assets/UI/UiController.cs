using Common;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour {
  [SerializeField] private Image reticle;

  void OnEnable() {
    EventController.OnAimingState += OnAimingStateChanged;
  }

  void OnDisable() {
    EventController.OnAimingState -= OnAimingStateChanged;
  }

  private void OnAimingStateChanged(bool isAiming) {
    reticle.gameObject.SetActive(isAiming);
  }
}