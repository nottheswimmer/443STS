using UnityEngine;


public class WeaponSensor : MonoBehaviour
{
    public float damage;
    public PlayerCamera player;

    private void OnCollisionEnter(Collision other)
    {
        if (player.IsAttacking())
            other.gameObject.SendMessage("ApplyDamage", damage * player.attackPower);
    }
}
