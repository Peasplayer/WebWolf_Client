using System.Net.WebSockets;
using Newtonsoft.Json;
using Spectre.Console;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;
using WebWolf_Client.Roles.RoleClasses;
using WebWolf_Client.Settings;
using WebWolf_Client.Ui;

namespace WebWolf_Client;

public class GameManager
{
    public static GameState State { get; private set; } = GameState.NoGame;
    public static InGameStateType InGameState { get; private set; } = InGameStateType.NoGame;

    public static void ChangeState(GameState newState)
    {
        State = newState;
        if (newState == GameState.InLobby)
        {
            UiHandler.ResetLobby();
        }
        else if (newState == GameState.InGame)
        {
            OnGameStart();
        }
    }
    
    public static void ChangeInGameState(InGameStateType newState)
    {
        InGameState = newState;

        if (newState == InGameStateType.NoGame)
            return;
        
        if (PlayerData.LocalPlayer.IsHost)
        {
            // Zeigt das passende Menü an
            UiHandler.RpcUiMessage(UiMessageType.DisplayInGameMenu);
            
            // Nacht-Ablauf
            if (newState == InGameStateType.Night)
            {
                Task.Delay(1000).Wait();
                foreach (var role in RoleManager.Roles)
                {
                    if (role.IsAliveRole)
                    {
                        RoleManager.RpcCallRole(role.RoleType);
                        Task.Delay(1000).Wait();
                    }
                }
                RpcStartNightOrDay(false);
            }
            // Tag-Ablauf
            else if (newState == InGameStateType.Day)
            {
                // Tote Spieler werden aufgedeckt
                var markedAsDead = PlayerData.Players.FindAll(player => player.IsMarkedAsDead);
                UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                    "Die Sonne geht auf...\nBei der morgendlichen Dorfversammlung fällt euch auf, dass...\n" 
                    + (markedAsDead.Count > 0 ? string.Join(", ", markedAsDead.ConvertAll(player => player.Name)) + 
                                                " gestorben " + (markedAsDead.Count > 1 ? "sind!": "ist!") : "alle wohlauf sind!"));
                PlayerData.RpcProcessDeaths();
                
                // Überprüft, ob das Spiel enden sollte ...
                if (CheckGameEnd())
                {
                    // ... und beendet es
                    RpcEndGame();
                    return;
                }
                
                // Abstimmung wird durchgeführt
                UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle, "Anschließend beratet ihr, die Dorfbewohner, euch...\nWer könnte ein Werwolf sein?");
                Task.Delay(1000).Wait();
                RpcStartVillageVote();
                
                // Überprüft, ob das Spiel enden sollte ...
                if (CheckGameEnd())
                {
                    // ... und beendet es
                    RpcEndGame();
                    return;
                }
                
                // Am Ende des Tages wird die nächste Nacht gestartet
                if (PlayerData.LocalPlayer.IsHost)
                {
                    RpcStartNightOrDay(true);
                }
            }
        }
    }

    public static void OnPlayerJoin(Packets.PlayerDataPattern data)
    {
        Program.DebugLog($"Player {data.Name} joined with ID {data.Id}");
        PlayerData.Players.Add(new PlayerData(data.Name, data.Id));

        // Falls ein Spieler vor dem Spiel-Start das Spiel verlässt ...
        if (GameManager.State == GameState.InLobby)
        {
            // ... wird die Anzeige erneuert
            UiHandler.DisplayLobby();
        }
    }
    
    public static void OnPlayerLeave(Packets.PlayerDataPattern data)
    {
        var player = PlayerData.GetPlayer(data.Id); 
        Program.DebugLog($"Player {player.Name} left with ID {player.Id}");
        PlayerData.Players.Remove(player);

        // Falls ein Spieler vor dem Spiel-Start das Spiel verlässt ...
        if (GameManager.State == GameState.InLobby)
        {
            // ... wird die Anzeige erneuert
            UiHandler.DisplayLobby();
        }
        // Falls ein Spieler während dem Spiel das Spiel verlässt ...
        else if (GameManager.State == GameState.InGame)
        {
            // ... und es der Host ist ...
            if (player.IsHost)
            {
                // ... wird das Spiel abgebrochen ...
                NetworkingManager.DisconnectionReason = "Der Host hat das Spiel verlassen!";
                NetworkingManager.Instance.Client.Stop(WebSocketCloseStatus.NormalClosure, "Host left the game");
                AnsiConsole.Write(new Rule("Der Host hat das Spiel verlassen!"));
                return;
            }

            // ... und es gerade Nacht ist ...
            if (InGameState == InGameStateType.Night)
            {
                if (PlayerData.LocalPlayer.IsHost)
                {
                    // ... hört der Host auf auf ihn zu warten
                    RoleManager.WaitingForRole.Remove(player.Id);
                    if (player.Role == RoleType.Werwolf)
                    {
                        // ... und er ein Werwolf war, wird die Abstimmung überprüft
                        ((Werwolf) RoleManager.GetRole(RoleType.Werwolf)).CalculateVictim();
                    }
                }
                
                // ... werden alle Abfragen nach Spielern erneuert
                UiHandler.ReRunPlayerPrompt();
            }
            // ... und es gerade Tag ist ...
            else if (InGameState == InGameStateType.Day)
            {
                // ... und die Abstimmung läuft ...
                if (VoteIsStarted)
                {
                    var toBeRemoved = new List<string>();
                    foreach (var pair in Votes)
                    {
                        if (pair.Value == player.Id)
                        {
                            if (pair.Key == PlayerData.LocalPlayer.Id)
                            {
                                // ... darf man erneut wählen, falls man für ihn gestimmt hat
                                HasVoted = false;
                            }
                            toBeRemoved.Add(pair.Key);
                        }
                    }
                    foreach (var key in toBeRemoved)
                    {
                        // ... werden alle Stimmen für ihn entfernt
                        Votes.Remove(key);
                    }
                    
                    // ... und die Abstimmungs-Anzeige wird erneuert
                    UiHandler.DisplayVillageVote();
                }
            }
        }
    }

    public static void OnGameStart()
    {
        foreach (var role in RoleManager.Roles)
        {
            role.InitRole();
        }

        if (PlayerData.LocalPlayer.IsHost)
        {
            // Rollen werden zugewiesen und angezeigt
            RoleManager.AssignRoles();
            while (PlayerData.Players.Find(player => player.Role == RoleType.NoRole) != null) ;
            UiHandler.RpcUiMessage(UiMessageType.DisplayCardReveal);
            
            // Anzahl der Rollen werden angezeigt
            var lines = "Es gibt folgende Rollen: ";
            foreach (var role in RoleManager.Roles)
            {
                lines += $"\n{RoleManager.GetPlayersWithRole(role.RoleType).Count} * {role.RoleType}";
            }
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle, lines);
            Task.Delay(1000).Wait();
            
            // Kurze Begrüßung
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle, "Nach einem anstrengenden Umzug nach Düsterwald\n" +
                                                                       "fallt ihr alle müde in eure Betten.\nUnd die erste Nacht beginnt...");
            Task.Delay(1000).Wait();
            
            // Die erste Nacht beginnt
            RpcStartNightOrDay(true);
        }
    }

    public static void RpcStartNightOrDay(bool isNight)
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.StartNightOrDay, JsonConvert.SerializeObject(new Packets.SimpleBoolean(isNight)))));
    }
    
    // Verzeichnis der Stimmen
    public static Dictionary<string, string> Votes = new Dictionary<string, string>();
    public static bool HasVoted;
    public static bool VoteIsStarted;
    
    // Host startet die Abstimmung
    public static void RpcStartVillageVote()
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.VillageVoteStart, "")));
        Votes.Clear();
        
        // Timer wird gestartet
        var timer = Task.Delay(60 * 1000);
        var validVoters = PlayerData.Players.Count(player => player.IsAlive);
        while (Votes.Count < validVoters)
        {
            if (timer.IsCompleted)
            {
                break;
            }
        }
        
        // Falls die Abstimmung noch läuft, wird sie abgebrochen
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId,
                PacketDataType.VillageVoteCanceled, "")));
        
        // Ergebnis wird berechnet
        var calcVotes = new Dictionary<string, int>();
        foreach (var vote in Votes)
        {
            calcVotes.TryAdd(vote.Value, 0);
            calcVotes[vote.Value]++;
        }

        calcVotes = calcVotes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        
        Task.Delay(1000).Wait();
        
        // Das Ergebnis der Abstimmung wird verkündet
        if (calcVotes.Count == 0 || (calcVotes.Count > 1 && calcVotes.Values.ToArray()[0] == calcVotes.Values.ToArray()[1]))
            VoteAnnounceVictim(null);
        else
            VoteAnnounceVictim(PlayerData.GetPlayer(calcVotes.First().Key));
    }

    public static void VoteAnnounceVictim(PlayerData? player)
    {
        if (player == null)
        {
            // Es wurde kein Opfer gefunden
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                $"Die Dorfbewohner haben entschieden!\nNiemand wurde zum Tode verurteilt!");
        }
        else
        {
            // Es wurde ein Opfer gefunden
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                $"Die Dorfbewohner haben entschieden!\n{player.Name} wurde zum Tode verurteilt!");
            Task.Delay(1000).Wait();
            
            if (PlayerData.LocalPlayer.IsHost)
            {
                // Host setzt das Opfer auf Tod
                player.RpcMarkAsDead();
                PlayerData.RpcProcessDeaths();
            }
        }
    }
    
    public enum GameState
    {
        NoGame,
        InLobby,
        InGame
    }

    public enum InGameStateType
    {
        NoGame,
        Day,
        Night
    }

    private static bool CheckGameEnd()
    {
        return false;
        
        // Zählt die lebenden Spieler nach Rollen
        int werwolves = PlayerData.Players.Count(player => player.IsAlive && player.Role == RoleType.Werwolf);
        int villagers = PlayerData.Players.Count(player => player.IsAlive && player.Role != RoleType.Werwolf);

        // Überprüft, ob alle Werwölfe tot sind
        if (werwolves == 0)
        {
            return true;
        }
        
        // Überprüft, ob es mehr oder gleich viele Werwölfe wie Dorfbewohner gibt 
        if (werwolves >= villagers)
        {
            return true;
        }

        return false;
    }

    private static void RpcEndGame()
    {
        int werwolves = PlayerData.Players.Count(player => player.IsAlive && player.Role == RoleType.Werwolf);
        int villagers = PlayerData.Players.Count(player => player.IsAlive && player.Role != RoleType.Werwolf);
        
        // Wenn die Dorfbewohner gewonnen haben, wird ihre Karte angezeigt
        if (werwolves == 0)
        {
            UiHandler.RpcUiMessage(UiMessageType.Clear);
            UiHandler.RpcUiMessage(UiMessageType.RenderCard, RoleType.Dorfbewohner.ToString());
            UiHandler.RpcUiMessage(UiMessageType.RenderText, "Die Dorfbewohner haben überlebt! Alle Werwölfe sind Tot.");
        }
        // Wenn die Werwölfe gewonnen haben, wird ihre Karte angezeigt 
        else if (werwolves >= villagers)
        {
            UiHandler.RpcUiMessage(UiMessageType.Clear);
            UiHandler.RpcUiMessage(UiMessageType.RenderCard, RoleType.Werwolf.ToString());
            UiHandler.RpcUiMessage(UiMessageType.RenderText, "Die Werwölfe haben alle Dorfbewohner gefressen!");
        }
    }
}