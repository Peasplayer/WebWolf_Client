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
        // Wenn die Aktion abgebrochen wird, wird das Opfer berechnet
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
                JsonConvert.SerializeObject(new SpecialPlayerCircle(UiHandler.PlayersToPlayerNames
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
        
        // Funktion wandet Spielernamen in Optionen mit den zugehörigen Stimmen um
        string PlayerToOption(PlayerData player) {
            var votes = Votes.Where(pair => pair.Value == player.Id).ToList().ConvertAll(pair => PlayerData.GetPlayer(pair.Key).Name);
            return player.Name + (votes.ToArray().Length > 0 ? $" (Votes: {string.Join(", ", votes)})" : "");
        }
        var playerList = PlayerData.Players.FindAll(player => player.Role != RoleType.Werwolf && player.IsAlive).ConvertAll(PlayerToOption);
        // Falls der Spieler noch nicht gewählt hat, wird er aufgefordert einen Spieler zu wählen
        if (!HasVoted)
        {
            if (CancelCheck(() => UiHandler.StartPlayerPrompt(renderPage, player => player.Role != RoleType.Werwolf && player.IsAlive,
                    "Wähle einen Spieler den du umbringen möchtest:", player =>
                    {
                        Program.DebugLog("Choice: " + player.Name);

                        // Stimme wird gesendet
                        HasVoted = true;
                        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                            new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.WerwolfVote, JsonConvert.SerializeObject(new Pakets.SimplePlayerId(player.Id)))));
                        
                        // User-Input endet, also wird dies signalisiert
                        RpcFinishedAction();
                    }, PlayerToOption))) return;
        }
        // Ansonsten wird dem Spieler das aktuelle Ergebnis gezeigt
        else
        {
            renderPage();
            foreach (var player in playerList)
            {
                AnsiConsole.WriteLine(player); 
            }
        }
    }

    public void CalculateVictim()
    {
        // Nur der Host berechnet das Opfer
        if (!PlayerData.LocalPlayer.IsHost)
            return;
        
        // Falls alle lebenden Werwölfe abgestimmt haben oder die Aktion abgebrochen wurde, wird das Opfer berechnet
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
            
            // Falls es keine Stimmen oder ein Unentschieden gibt, wird kein Opfer gewählt
            if (calcVotes.Count == 0 || (calcVotes.Count > 1 && calcVotes.Values.ToArray()[0] == calcVotes.Values.ToArray()[1]))
                RpcAnnounceVictim("");
            // Ansonsten wird das Opfer verkündet
            else
            {
                var playerId = calcVotes.First().Key;
                RpcAnnounceVictim(playerId);
                PlayerData.GetPlayer(playerId).RpcMarkAsDead();
            }
        }
    }
    
    // Das Opfer wird alle Werwölfen verkündet
    private void RpcAnnounceVictim(string playerId)
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPaket(NetworkingManager.Instance.CurrentId,
                PaketDataType.WerwolfAnnounceVictim,
                JsonConvert.SerializeObject(
                    new Pakets.SimplePlayerId(playerId)))));
    }

    // Werwölfen wird das Ergebnis ihrer Abstimmung gezeigt
    public void AnnounceVictim(string playerId)
    {
        UiHandler.CancelPrompt();
        AnsiConsole.Clear();
        UiHandler.RenderCard(RoleType.Werwolf, "", 10);
        AnsiConsole.WriteLine("\n");

        var player = PlayerData.GetPlayer(playerId);
        // Je nachdem ob ein Opfer gefunden wurde, wird dies angezeigt
        if (player == null)
            UiHandler.LocalUiMessage(UiMessageType.RenderText, "Es wurde kein Opfer gefunden!");
        else
            UiHandler.LocalUiMessage(UiMessageType.RenderText, $"Ihr habt {player.Name} als Opfer ausgewählt!");
            
        Task.Delay(1000).Wait();
        UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
        Task.Delay(1000).Wait();
        
        // Die Aktion endet
        RpcFinishedAction();
    }
}