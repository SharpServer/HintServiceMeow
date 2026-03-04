namespace HintServiceExample
{
    using System;
    using HintServiceMeow.Core.Enum;
    using HintServiceMeow.Core.Extension;
    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities;
    using HintServiceMeow.UI.Utilities;
    using LabApi.Events.Arguments.PlayerEvents;
    using UnityEngine;
    using Hint = HintServiceMeow.Core.Models.Hints.Hint;

    /// <summary>
    /// This is an Exiled only example of how to create a simple ui for players using Hint and PlayerDisplay.
    /// </summary>
    public class Plugin : LabApi.Loader.Features.Plugins.Plugin
    {
        public override string Name => "HintServiceExample";
        public override string Author => "MeowServer";
        public override string Description => "A example plugin for HSM";
        public override Version Version { get; } = new Version(5, 4, 4);
        public override Version RequiredApiVersion { get; } = Version.Parse(LabApi.Features.LabApiProperties.CompiledVersion);
        public override void Enable()
        {
            LabApi.Events.Handlers.PlayerEvents.Joined += EventHandler.OnVerified;
            LabApi.Events.Handlers.ServerEvents.WaitingForPlayers += EventHandler.OnWaitingForPlayer;
        }

        public override void Disable()
        {
            LabApi.Events.Handlers.PlayerEvents.Joined -= EventHandler.OnVerified;
            LabApi.Events.Handlers.ServerEvents.WaitingForPlayers -= EventHandler.OnWaitingForPlayer;
        }
    }

    public static class EventHandler
    {
        public static void OnWaitingForPlayer()
        {
            //To better demonstrate the hint, we will hide the lobby timer
            GameObject.Find("StartRound").transform.localScale = Vector3.zero;
        }

        public static void OnVerified(PlayerJoinedEventArgs ev)
        {
            // Hint is a simple hint that can be shown on the player's screen.
            Hint hint = new Hint
            {
                Text = "Hello World" // You can set the properties of the hint in a pair of braces({})
            };

            // You can set the properties of the hint like this:
            hint.FontSize = 40;
            hint.YCoordinate = 700;
            hint.Alignment = HintAlignment.Left;
            // After setting the properties, you don't need to use method to call for an update. All update will be done automatically by the HSM(HintServiceMeow).

            // You can show a hint to a player by adding it to the player's PlayerDisplay.
            // You can also remove it by removing it from the PlayerDisplay.
            PlayerDisplay playerDisplay = PlayerDisplay.Get(ev.Player);
            playerDisplay.AddHint(hint);
            playerDisplay.RemoveHint(hint);

            // You can also use extension method to make it easier
            ev.Player.AddHint(hint); // This is equivalent to playerDisplay.AddHint(hint);
            ev.Player.RemoveHint(hint); // This is equivalent to playerDisplay.RemoveHint(hint);

            // If you only want to show a hint temporarily, you can use ShowHint
            playerDisplay.ShowHint(hint, 12f); // This show a hint for 12 second, then hide it.

            // DynamicHint is a hint that can automatically arrange itself to avoid overlapping with other hints.
            DynamicHint dynamicHint = new DynamicHint
            {
                Text = "Hello Dynamic Hint",
                TargetX = 100f, // You can choose to set a property or not to in a braces. All properties have a default value, so you can just set the properties you want to change.
            };

            playerDisplay.AddHint(dynamicHint);

            // PlayerUI::CommonHint is a set of preset hints that help you to show hints easily
            PlayerUI ui = PlayerUI.Get(ev.Player);
            ui.CommonHint.ShowRoleHint("SCP173", ["Kill all humans", "Use your skills"]);
            ui.CommonHint.ShowMapHint("Heavy Containment Zone", "The place where most SCPs spawn");
            ui.CommonHint.ShowItemHint("Keycard", "Used to open doors");
            ui.CommonHint.ShowOtherHint("The server is starting!");

            // Read CoreFeatures.md to learn more
        }
    }
}
