namespace WebWolf_Client.Networking;

public enum PacketDataType : uint
{
    // Spiel-System
    Join,
    Leave,
    SyncLobby,
    SetHost,
    StartGame,
    EndGame,
    StartNightOrDay,
    
    // Rollen
    SetRole,
    CallRole,
    RoleFinished,
    RoleCanceled,
    
    // Werwölfe
    WerwolfVote,
    WerwolfAnnounceVictim,
    
    // Spieler
    PlayerMarkedAsDead,
    PlayerUnmarkedAsDead,
    PlayerProcessDeaths,
    
    // Abstimmung
    VillageVoteStart,
    VillageVoteVoted,
    VillageVoteCanceled,
    VillageVoteAnnounceVictim,
    
    // UI
    UiMessage,
    UiMessageFinished,
}