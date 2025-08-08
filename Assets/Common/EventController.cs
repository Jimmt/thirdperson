using System;

namespace Common {
  public static class EventController {
    public static event Action<bool> OnAimingState;
    public static event Action<string> OnWeaponSelected;

    public static void TriggerAimingStateChanged(bool isAiming) {
      OnAimingState?.Invoke(isAiming);
    }
    public static void TriggerWeaponSelected(string weaponName) {
      OnWeaponSelected?.Invoke(weaponName);
    }
  }
}