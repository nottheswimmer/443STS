using UnityEngine;


public class WeaponSensor : MonoBehaviour
{
    public float damage;

    private void OnCollisionEnter(Collision other)
    {
        other.gameObject.SendMessage("ApplyDamage", damage);
    }
}
