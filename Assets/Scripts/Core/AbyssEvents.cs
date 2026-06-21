using System;

// Hub de eventos estáticos: ningún sistema referencia directamente a otro,
// toda comunicación pasa por aquí (publicación y suscripción).
public static class AbyssEvents
{
    public static event Action<int>   OnComboChanged;
    public static event Action<int>   OnNearMiss;
    public static event Action<float> OnSurgeMeterChanged;
    public static event Action        OnSurgeActivated;
    public static event Action        OnSurgeDeactivated;
    public static event Action<float> OnImpact;
    public static event Action        OnZoneTransition;

    public static void TriggerComboChanged(int tier)     => OnComboChanged?.Invoke(tier);
    public static void TriggerNearMiss(int shards)       => OnNearMiss?.Invoke(shards);
    public static void TriggerSurgeMeterChanged(float t) => OnSurgeMeterChanged?.Invoke(t);
    public static void TriggerSurgeActivated()           => OnSurgeActivated?.Invoke();
    public static void TriggerSurgeDeactivated()          => OnSurgeDeactivated?.Invoke();
    public static void TriggerImpact(float intensity)    => OnImpact?.Invoke(intensity);
    public static void TriggerZoneTransition()            => OnZoneTransition?.Invoke();
}
