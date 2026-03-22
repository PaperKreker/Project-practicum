using System.Collections.Generic;

public enum RunState
{
    MainMenu,
    Map,
    Battle,
    Shop,
    Rest,
    Victory,
    GameOver,
}

// All mutable run-level state
public class RunData
{
    public int PlayerHp;
    public int PlayerMaxHp;
    public int Gold;
    public int CurrentNodeIndex;

    public bool CurrentNodeCompleted = false;

    public List<object> ActiveSigils = new List<object>();
}
