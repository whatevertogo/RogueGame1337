using UnityEngine;

/// <summary>
/// Pickup interface: any pickup object should implement this to support AutoPickupComponent detection
/// </summary>
public interface IPickup
{
    void OnEnterPickupRange(AutoPickupComponent picker);
}
