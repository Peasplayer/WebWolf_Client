using Spectre.Console;
using WebWolf_Client.Settings;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Amor : Role
{
    public override RoleType RoleType => RoleType.Amor;
    public override bool IsAliveRole => true;

    private bool CreatedCouple;

    public override void InitRole()
    {
        CreatedCouple = false;
    }

    protected override void StartAction()
    {
        var playersInLove = PlayerData.Players.FindAll(player => player is { InLove: true, IsAlive: true });
        if (!CreatedCouple || (SettingsManager.AmorMultipleCouples.Value && playersInLove.Count > 0))
        {
            UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, "Der Amor (Du) erwacht...");
            if (CancelCheck(() => UiHandler.StartPlayerPrompt(() =>
                    {
                        AnsiConsole.Clear();
                        UiHandler.RenderCard(RoleType.Amor, "", 10);
                        AnsiConsole.WriteLine("\n");
                        UiHandler.LocalUiMessage(UiMessageType.RenderText, "Wähle zwei Spieler, die sich ineinander verlieben sollen\n");
                    },
                    player => player.IsAlive,
                    "1. Verliebter:",
                    lover1 =>
                    {
                        if (CancelCheck(() => UiHandler.StartPlayerPrompt(() =>
                                {
                                    AnsiConsole.Clear();
                                    UiHandler.RenderCard(RoleType.Amor, "", 10);
                                    AnsiConsole.WriteLine("\n");
                                },
                                player => player.IsAlive && player.Id != lover1.Id,
                                "2. Verliebter:",
                                lover2 =>
                                {
                                    if (CancelCheck(RpcFinishedAction)) return;

                                    UiHandler.LocalUiMessage(UiMessageType.RenderText, $"{lover1.Name} und {lover2.Name} sind nun ineinander verliebt.");
                                    CreatedCouple = true;
                                    lover1.RpcSetInLove();
                                    lover2.RpcSetInLove();
                                    Task.Delay(1000).Wait();
                                    // Aktion für den Armor endet
                                    UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
                                    
                                    // Die Verliebten Spieler erhalten eine Nachricht
                                    UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle, 
                                        $"Dich hat der Pfeil Amors getroffen.\nAls du {lover2.Name} erblickst, verliebst du dich sofort.", 
                                        new List<string>{lover1.Id});
                                    UiHandler.RpcUiMessage(UiMessageType.DisplayInGameMenu, 
                                        receivers: new List<string>{lover1.Id});
                                    UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle, 
                                        $"Dich hat der Pfeil Amors getroffen.\nAls du {lover1.Name} erblickst, verliebst du dich sofort.", 
                                        new List<string>{lover2.Id});
                                    UiHandler.RpcUiMessage(UiMessageType.DisplayInGameMenu, 
                                        receivers: new List<string>{lover2.Id});
                                    
                                    // Aktion ist beendet
                                    RpcFinishedAction();
                                }))) return;
                    }))) return;
        }
        else
        {
            RpcFinishedAction();
            if (SettingsManager.AmorMultipleCouples.Value)
                UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, "Der Amor (Du) schläft weiter\nLiebe ist noch im Dorf vorhanden");
            RpcFinishedAction();
        }
    }
}