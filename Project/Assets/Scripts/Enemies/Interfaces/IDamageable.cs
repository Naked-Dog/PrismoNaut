using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    bool Damage(int damageAmount);

    void Die();

}