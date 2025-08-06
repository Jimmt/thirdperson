using System;

namespace Common {
  public static class EventController {
    public static event Action<bool> OnAimingState;

    public static void TriggerAimingStateChanged(bool isAiming) {
      OnAimingState?.Invoke(isAiming);
    }
  }
}