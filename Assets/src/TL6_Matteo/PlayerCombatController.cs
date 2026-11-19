using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [SerializeField] private PlayerVisualController _visualController;
    [SerializeField] private float _secondsBetweenShots = 0.18f;

    private IPlayerCombatInput _combatInput;
    private FireCooldown _fireCooldown;

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
            }
        }
    }
}
