using UnityEngine;

// 3. Living Cargo (People/Pets/Outlaws)
[CreateAssetMenu(menuName = "Items/LivingCargo")]
public class LivingCargo : CollectibleItem { public bool isHostile; public string passengerName; }
