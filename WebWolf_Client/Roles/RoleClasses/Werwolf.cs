using Newtonsoft.Json;
using Spectre.Console;
using WebWolf_Client.Networking;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Werwolf : Role
{
    public static string LastVicitmId;
    
    public override RoleType RoleType => RoleType.Werwolf;
    public override bool IsAliveRole => true;

    public Dictionary<string, string> Votes = new Dictionary<string, string>();
    public bool HasVoted;
    private bool VoteDone;

    protected override void CancelAction() { }

    public override void AfterCancel()
    {
        CalculateVictim();
    }

    public override void ResetAction()
    {
        Votes.Clear();
        HasVoted = false;
        VoteDone = false;
    }

    protected override void StartAction()
    {
        if (CancelCheck(() => UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, 
                JsonConvert.SerializeObject(new UiMessageClasses.SpecialPlayerCircle(UiHandler.PlayersToPlayerNames
                    (PlayerData.Players, true, true, "red", RoleType.Werwolf), 
                    "Die Werwölfe (Du) erwachen..."))))) return;
        if (CancelCheck(() => Task.Delay(1000).Wait())) return;
        
        if (CancelCheck(SelectVictim)) return;
        //AnsiConsole.Write(new Align(new Panel(String.Join(", ", Program.DebugNames)).Header("Spieler"), HorizontalAlignment.Center));
    }

    public void SelectVictim()
    {
        var renderPage = () =>
        {
            UiHandler.CancelPrompt();
            AnsiConsole.Clear();
            UiHandler.RenderCard(RoleType.Werwolf, "", 10);
            AnsiConsole.WriteLine("\n");
        };
        
        string PlayerToOption(PlayerData player) {
            var votes = Votes.Where(pair => pair.Value == player.Id).ToList().ConvertAll(pair => PlayerData.GetPlayer(pair.Key).Name);
            return player.Name + (votes.ToArray().Length > 0 ? $" (Votes: {string.Join(", ", votes)})" : "");
        }
        var playerList = PlayerData.Players.FindAll(player => player.Role != RoleType.Werwolf && player.IsAlive).ConvertAll(PlayerToOption);
        if (!HasVoted)
        {
            if (CancelCheck(() => UiHandler.StartPlayerPrompt(renderPage, player => player.Role != RoleType.Werwolf && player.IsAlive,
                    "Wähle einen Spieler den du umbringen möchtest:", CastVote, PlayerToOption))) return;
        }
        else
        {
            renderPage();
            foreach (var player in playerList)
            {
                AnsiConsole.WriteLine(player); 
            }
        }
    }

    private void CastVote(PlayerData player)
    {
        Program.DebugLog("Choice: " + player.Name);

        HasVoted = true;
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.WerwolfVote, JsonConvert.SerializeObject(new Packets.SimplePlayerId(player.Id)))));
        RpcFinishedAction();
    }

    public void CalculateVictim()
    {
        if (!PlayerData.LocalPlayer.IsHost)
            return;
        
        if ((Votes.Count == PlayerData.Players.Count(player => player is { Role: RoleType.Werwolf, IsAlive: true }) ||
             IsActionCancelled) && !VoteDone)
        {
            VoteDone = true;
            var calcVotes = new Dictionary<string, int>();
            foreach (var vote in Votes)
            {
                calcVotes.TryAdd(vote.Value, 0);
                calcVotes[vote.Value]++;
            }

            calcVotes = calcVotes.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                                
            if (calcVotes.Count == 0 || (calcVotes.Count > 1 && calcVotes.Values.ToArray()[0] == calcVotes.Values.ToArray()[1]))
                RpcAnnounceVictim("");
            else
            {
                var playerId = calcVotes.First().Key;
                RpcAnnounceVictim(playerId);
                PlayerData.GetPlayer(playerId).RpcMarkAsDead();
            }
        }
    }
    
    private void RpcAnnounceVictim(string playerId)
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId,
                PacketDataType.WerwolfAnnounceVictim,
                JsonConvert.SerializeObject(
                    new Packets.SimplePlayerId(playerId)))));
    }

    // Werwölfen wird das Ergebnis ihrer Abstimmung gezeigt
    public void AnnounceVictim(string playerId)
    {
        UiHandler.CancelPrompt();
        AnsiConsole.Clear();
        UiHandler.RenderCard(RoleType.Werwolf, "", 10);
        AnsiConsole.WriteLine("\n");

        var player = PlayerData.GetPlayer(playerId);
        if (player == null)
            UiHandler.LocalUiMessage(UiMessageType.RenderText, "Es wurde kein Opfer gefunden!");
        else
            UiHandler.LocalUiMessage(UiMessageType.RenderText, $"Ihr habt {player.Name} als Opfer ausgewählt!");
            
        Task.Delay(1000).Wait();
        UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
        Task.Delay(1000).Wait();
        
        RpcFinishedAction();
    }
}