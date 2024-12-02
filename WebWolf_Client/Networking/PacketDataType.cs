namespace WebWolf_Client.Networking;

public enum PacketDataType : uint
{
    Join,
    Leave,
    SyncLobby,
    SetHost,
    StartGame,
    SetRole,
    StartNightOrDay,
    CallRole,
    RoleFinished,
    RoleCanceled
}