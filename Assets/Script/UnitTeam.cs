using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public class UnitTeam : MonoBehaviour
{
    public Team team = Team.Player;
}
