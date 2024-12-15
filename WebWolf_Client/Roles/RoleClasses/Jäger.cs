using Spectre.Console;
using WebWolf_Client.Settings;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Jäger : Role
{
    public override RoleType RoleType => RoleType.Jäger;
    public override bool IsAliveRole => false;
    public override void ResetAction() {}

    protected override void StartAction()
    {
        if (CancelCheck(() => UiHandler.StartPlayerPrompt(() =>
                {
                    AnsiConsole.Clear();
                    UiHandler.RenderCard(RoleType.Jäger, "", 10);
                    AnsiConsole.WriteLine("\n");
                    UiHandler.LocalUiMessage(UiMessageType.RenderText,"Du bist gestorben.");
                },
                player => player is {IsLocal: false, IsAlive: true},
                "Wen möchtest du mit in den Tod reisen?",
                victim =>
                {
                    if (CancelCheck(RpcFinishedAction)) return;
                        
                    // Opfer wird markiert als tot
                    victim.RpcMarkAsDead();
                        
                    UiHandler.LocalUiMessage(UiMessageType.RenderText, $"Du hast {victim.Name} erschossen.");
                    RpcFinishedAction();
                }))) return;
    }

    public static void CallJäger()
    {
        RoleManager.RpcCallRole(RoleType.Jäger);
        var markedAsDead = PlayerData.Players.FindAll(player => player.IsMarkedAsDead);
        if (markedAsDead.Count > 0)
        {
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                "Mit seinem letzten Atemzug erschießt der Jäger...\n"
                + markedAsDead.First().Name + "!");
            Task.Delay(1000).Wait();
            // Rolle wird offenbart, sofern dies eingestellt ist
            if (SettingsManager.RevealRoleOnDeath.Value)
            {
                UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                    string.Join("\n", markedAsDead.ConvertAll(player => $"{player.Name} war {player.Role}")));
                Task.Delay(2000).Wait();
            }
            PlayerData.RpcProcessDeaths();
        }
    }
}