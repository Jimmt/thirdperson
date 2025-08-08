using Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour {
  [SerializeField] private Image reticle;
  [SerializeField] private GameObject topLeftText;

  void OnEnable() {
    EventController.OnAimingState += OnAimingStateChanged;
    EventController.OnWeaponSelected += OnWeaponSelected;
  }

  void OnDisable() {
    EventController.OnAimingState -= OnAimingStateChanged;
    EventController.OnWeaponSelected -= OnWeaponSelected;
  }

  private void OnAimingStateChanged(bool isAiming) {
    reticle.gameObject.SetActive(isAiming);
  }
  
  private void OnWeaponSelected(string weaponName) {
    topLeftText.GetComponent<TextMeshProUGUI>().text = "Selected: " + weaponName + " (Q to switch)";
  }
}