// Keeping the logic here (not in BattleController) makes it easy to test
// and keeps BattleController focused on mechanics.
public static class VictoryChecker
{
    public static bool IsGameOver(int playerHp) => playerHp <= 0;

    public static bool IsBattleWon(int enemyHp) => enemyHp <= 0;

    public static bool IsRunComplete(int actsCompleted) => actsCompleted >= 3;
}
