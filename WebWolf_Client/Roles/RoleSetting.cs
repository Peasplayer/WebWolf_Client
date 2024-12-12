namespace WebWolf_Client.Roles;

public class RoleSetting
{
    public static List<RoleSetting> RoleSettings = new List<RoleSetting>()
    {
        new RoleSetting(RoleType.Werwolf, 2),
        new RoleSetting(RoleType.Hexe, 0),
        new RoleSetting(RoleType.Seherin, 1)
    };

    public static int GetMaxAmount(RoleType role)
    {
        return RoleSettings.Find(rs => rs.Role == role)?.MaxAmount ?? 0;
    }
    
    public RoleType Role;
    public int MaxAmount;

    public RoleSetting(RoleType role, int maxAmount)
    {
        Role = role;
        MaxAmount = maxAmount;
    }
}