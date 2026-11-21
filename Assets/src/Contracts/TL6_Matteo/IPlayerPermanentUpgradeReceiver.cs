namespace EscapeThe90s.Contracts
{
    public interface IPlayerPermanentUpgradeReceiver
    {
        void SetPermanentSpeedBonus(float totalBonus);
        void SetPermanentAttackBonus(float totalBonus);
    }
}