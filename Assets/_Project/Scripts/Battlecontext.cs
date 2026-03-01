// Mutable battle state passed into every EnemyEffect hook.
// Effects read and write this rather than talking to BattleController directly.
public class BattleContext
{
    public HandController Hand;
    public int PlayerHp;
    public int EnemyDamage;
    public int Discards;

    // Set by SuitNoDamage (Fox); null means no suit is blocked
    public Suit? BlockedDamageSuit;

    // Effects can call this to trigger a UI refresh
    public System.Action RequestUIRefresh;
}
