using UnityEngine;
using EscapeThe90s.Contracts;

public class PlayerCombatController : MonoBehaviour
{
    [SerializeField] private PlayerVisualController _visualController;
    [SerializeField] private float _secondsBetweenShots = 0.18f;
    [SerializeField] private PlayerProjectile _projectilePrefab;

    private IPlayerCombatInput _combatInput;
    private FireCooldown _fireCooldown;
    private int _permanentAttackBonus = 0;

    public void SetPermanentAttackBonus(float totalBonus)
    {
        _permanentAttackBonus =
            Mathf.Max(0, Mathf.RoundToInt(totalBonus));
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
        if (_combatInput == null || _visualController == null || _projectilePrefab == null)
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

        projectile.Initialize(_visualController.AimDirection, _permanentAttackBonus);
    }
}
