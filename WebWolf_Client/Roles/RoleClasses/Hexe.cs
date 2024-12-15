using Spectre.Console;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Hexe : Role
{
    public override RoleType RoleType => RoleType.Hexe;
    public override bool IsAliveRole => true;
    
    // Ob der Heiltrank noch verfügbar ist in diesem Spiel
    private bool HealingPotionAvailable = true;
    // Ob der Gifttrank noch verfügbar ist in diesem Spiel
    private bool PoisonPotionAvailable = true;

    public override void InitRole()
    {
        HealingPotionAvailable = true;
        PoisonPotionAvailable = true;
    }

    protected override void StartAction()
    {
        if (CancelCheck(() => UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, "Die Hexe (Du) erwacht..."))) return;
        if (CancelCheck(() => Task.Delay(1000).Wait())) return;
        
        if (CancelCheck(() =>
            {
                AnsiConsole.Clear();
                UiHandler.RenderCard(RoleType.Hexe, "", 10);
                AnsiConsole.WriteLine("\n");
            })) return;
        
        // Falls ein Opfer vorhanden ist, bekommt die Hexe es gesagt
        var lastVictim = PlayerData.GetPlayer(Werwolf.LastVicitmId);
        if (CancelCheck(() => UiHandler.LocalUiMessage(UiMessageType.RenderText, lastVictim == null ? "Es gibt kein Opfer." : $"Das Opfer der Werwölfe ist {lastVictim.Name}."))) return;
        Task.Delay(1000).Wait();
        
        // Falls der Heiltrank noch nicht genutzt wurde und es ein Opfer gab, kann die Hexe ihn nutzen
        if (HealingPotionAvailable && lastVictim != null)
        {
            bool useHealPotion = false;
            if (CancelCheck(() => useHealPotion = UiHandler.Prompt(
                    new ConfirmationPrompt("Möchtest du das Opfer der Werwölfe heilen?")))) return;
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
        if (PoisonPotionAvailable)
        {
            if (CancelCheck(() =>
                {
                    AnsiConsole.Clear();
                    UiHandler.RenderCard(RoleType.Hexe, "", 10);
                    AnsiConsole.WriteLine("\n");
                })) return;
            
            bool usePoisonPotion = false;
            if (CancelCheck(() => usePoisonPotion = UiHandler.Prompt(new ConfirmationPrompt("Möchtest du deinen Gifttrank einsetzen?")))) return;
            if (usePoisonPotion)
            {
                // Hexe sucht ein Opfer aus
                if (CancelCheck(() => UiHandler.StartPlayerPrompt(() =>
                        {
                            AnsiConsole.Clear();
                            UiHandler.RenderCard(RoleType.Hexe, "", 10);
                            AnsiConsole.WriteLine("\n");
                        },
                        player => player is { IsLocal: false, IsAlive: true },
                        "Wähle ein Opfer aus:", victim =>
                        {
                            // User-Input endet, also wird dies signalisiert
                            if (CancelCheck(RpcFinishedAction)) return;
                
                            // Der Trank wird als gebraucht markiert
                            PoisonPotionAvailable = false;
                    
                            // Der Spieler wird umgebracht
                            victim.RpcMarkAsDead();
                            UiHandler.LocalUiMessage(UiMessageType.RenderText, $"{victim.Name} wurde vergiftet");
                        }))) return;
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
        
        // Aktion endet
        RpcFinishedAction();
    }
}

