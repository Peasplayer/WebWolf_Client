using Spectre.Console;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Hexe : Role
{
    private bool HealingPotionAvailable = true;
    private bool PoisonPotionAvailable = true;
    public override RoleType RoleType => RoleType.Hexe;

    public override bool IsActiveRole => true;

    public override void ResetAction() { }
    
    protected override void StartAction()
    {
        if (CancelCheck(() =>UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, "Die Hexe (Du) erwacht..."))) return;
        if (CancelCheck(() => Task.Delay(1000).Wait())) return;
        
        if (CancelCheck(AnsiConsole.Clear)) return;
        if (CancelCheck(() => UiHandler.RenderCard(RoleType.Hexe, "", 10))) return;
        if (CancelCheck(() => AnsiConsole.WriteLine("\n"))) return;
        
        var lastVictim = PlayerData.GetPlayer(Werwolf.LastVicitmId);
        // Falls ein Opfer vorhanden ist, bekommt die Hexe es gesagt
        if (CancelCheck(() => UiHandler.LocalUiMessage(UiMessageType.RenderText, lastVictim == null ? "Es gibt kein Opfer." : $"Das Opfer der Werwölfe ist {lastVictim.Name}."))) return;
        
        // Falls der Heiltrank noch nicht genutzt wurde und es ein Opfer gab, kann die Hexe ihn nutzen
        if (HealingPotionAvailable && lastVictim != null)
        {
            bool useHealPotion = false;
            if (CancelCheck(() => useHealPotion = UiHandler.Prompt(
                    new ConfirmationPrompt("Möchtest du es heilen?")))) return;
            // Falls sie es heilen möchte, wird es geheilt
            if (useHealPotion)
            {
                // Der Trank wird als gebraucht markiert
                HealingPotionAvailable = false;
                    
                // Der Spieler wird wiederbelebt
                lastVictim.RpcUnmarkAsDead();
                UiHandler.LocalUiMessage(UiMessageType.RenderText, $"{lastVictim.Name} wurde geheilt.");
            }
        }

        // Falls der Gift-Trank noch nicht genutzt wurde, kann die Hexe ihn nutzen
        if (!PoisonPotionAvailable)
        {
            bool usePoisonPotion = false;
            if (CancelCheck(() => usePoisonPotion = UiHandler.Prompt(new ConfirmationPrompt("Möchtest du deinen [yellow] Gifttrank [/] einsetzen?")))) return;
            if (usePoisonPotion)
            {
                var playerName = "";
                if (CancelCheck(() => playerName = UiHandler.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Wähle ein Opfer aus:")
                            .PageSize(10)
                            .AddChoices(PlayerData.Players.FindAll(p => p is { IsAlive: true, IsLocal: false })
                                .ConvertAll(p => p.Name))))) return;
                // User-Input endet, also wird dies signalisiert
                if (CancelCheck(RpcFinishedAction)) return;
                
                var victim = PlayerData.Players.FirstOrDefault(p => p.Name == playerName);
                if (victim != null)
                {
                    // Der Trank wird als gebraucht markiert
                    PoisonPotionAvailable = false;
                    
                    // Der Spieler wird umgebracht
                    victim.RpcMarkAsDead();
                    UiHandler.LocalUiMessage(UiMessageType.RenderText, $"{victim.Name} wurde [red] vergiftet [/]");
                }
            }
            else
                // User-Input endet, also wird dies signalisiert
                if (CancelCheck(RpcFinishedAction)) return;
        }
        else
        {
            // User-Input endet, also wird dies signalisiert
            if (CancelCheck(RpcFinishedAction)) return;
        }
        
        RpcFinishedAction();
    }
}

