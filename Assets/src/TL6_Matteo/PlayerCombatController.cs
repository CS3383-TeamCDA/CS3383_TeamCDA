using UnityEngine;
using EscapeThe90s.Contracts;

public class PlayerCombatController : MonoBehaviour, IDependent<IMetaProgressionManager>
{
    [SerializeField] private PlayerVisualController _visualController;
    [SerializeField] private float _secondsBetweenShots = 0.18f;
    [SerializeField] private PlayerProjectile _projectilePrefab;

    private IPlayerCombatInput _combatInput;
    private FireCooldown _fireCooldown;
    private IMetaProgressionManager _progression;

    public void Inject(IMetaProgressionManager dependency)
    {
        _progression = dependency;
    }

    private void Awake()
    {
        if(_visualController == null)
            _visualController = GetComponent<PlayerVisualController>();

        _combatInput = GetComponent<IPlayerCombatInput>();
        _fireCooldown = new FireCooldown(_secondsBetweenShots);
    }

    private void Update()
    {
        if (_combatInput == null || _visualController == null)
            return;

        if (_fireCooldown.CanFire(_combatInput.FireHeld, Time.time))
        {
            if (_visualController.TryPlayFireAnimation())
            {
                _fireCooldown.RecordSuccessfulShot(Time.time);
                SpawnProjectile();
            }
        }
    }

    private void SpawnProjectile()
    {
        if (_projectilePrefab == null)
            return;

        PlayerProjectile projectile = Instantiate(
            _projectilePrefab,
            _visualController.MuzzlePosition,
            Quaternion.identity
        );

        float attackBonus = _progression?.GetEffectValue(UpgradeType.Attack) ?? 0f;
        projectile.Initialize(_visualController.AimDirection, Mathf.RoundToInt(attackBonus));
    }
}
