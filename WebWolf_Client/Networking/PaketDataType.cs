namespace WebWolf_Client.Networking;

public enum PaketDataType : uint
{
    // System
    Join,
    Leave,
    SyncLobby,
    SetHost,
    Disconnect,
    
    // Spiel
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
    PlayerInLove,
    
    // Abstimmung
    VillageVoteStart,
    VillageVoteVoted,
    VillageVoteCanceled,
    
    // UI
    UiMessage,
    UiMessageFinished,
}