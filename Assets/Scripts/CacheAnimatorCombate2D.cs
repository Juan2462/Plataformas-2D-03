using UnityEngine;

/// <summary>
/// Cachea parámetros del Animator una sola vez (evita recorrer animator.parameters por ataque).
/// </summary>
public class CacheAnimatorCombate2D : MonoBehaviour
{
    public bool TieneAttack { get; private set; }
    public bool TieneAttackUp { get; private set; }
    public bool TieneAttackDown { get; private set; }
    public bool TieneAttackFinisher { get; private set; }
    public bool TieneComboIndex { get; private set; }

    private void Awake()
    {
        var animator = GetComponent<Animator>();
        if (animator == null) return;

        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger)
            {
                if (p.name == "Attack" || p.name == "attack") TieneAttack = true;
                else if (p.name == "AttackUp") TieneAttackUp = true;
                else if (p.name == "AttackDown") TieneAttackDown = true;
                else if (p.name == "AttackFinisher") TieneAttackFinisher = true;
            }
            else if (p.type == AnimatorControllerParameterType.Float && p.name == "ComboIndex")
                TieneComboIndex = true;
        }
    }
}
