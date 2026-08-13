namespace HintServiceMeow.Plugin
{
    using System;
    using HintServiceMeow.Core.Utilities;
    using HintServiceMeow.Core.Utilities.Patch;
    using HintServiceMeow.Core.Utilities.Tools;
    using HintServiceMeow.UI.Utilities;

#if !EXILED
    using LabApi.Events.Arguments.PlayerEvents;
    using LabApi.Events.Handlers;
    using LabApi.Features;
    using LabApi.Loader;
    using LabApi.Loader.Features.Plugins.Enums;
#endif

#if EXILED
    internal class Plugin : Exiled.API.Features.Plugin<PluginConfig>
#else
    internal class Plugin : LabApi.Loader.Features.Plugins.Plugin
#endif
    {
        public static Plugin Instance { get; private set; } = null!;

#if EXILED
        public override string Name => "HintServiceMeow";

        public override string Author => "MeowServer";

        public override Version Version => new(5, 5, 1);

        public override Version RequiredExiledVersion => new(9, 6, 0);

        public override Exiled.API.Enums.PluginPriority Priority => Exiled.API.Enums.PluginPriority.Highest;
#else
        public override string Name => "HintServiceMeow";

        public override string Author => "MeowServer";

        public override Version Version => new(5, 5, 0);

        public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

        public override string Description => "A hint framework";

        public override LoadPriority Priority => LoadPriority.Highest;

        public PluginConfig Config { get; private set; } = null!;

        public override void LoadConfigs()
        {
            base.LoadConfigs();

            Config = this.LoadConfig<PluginConfig>("config.yml") ?? throw new NullReferenceException("Could not load plugin config!");
        }
#endif

#if EXILED
        public override void OnEnabled()
#else
        public override void Enable()
#endif
        {
            Instance = this;

#if EXILED
            Exiled.Events.Handlers.Player.Left += OnLeft;

            // Left only fires from CustomNetworkManager.OnServerDisconnect, so NPCs and dummies --
            // which are destroyed without ever disconnecting -- would leak their PlayerDisplay
            // forever. Destroying is patched onto ReferenceHub.OnDestroy and covers both.
            Exiled.Events.Handlers.Player.Destroying += OnDestroying;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
#else
            ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
            PlayerEvents.Left += OnLeft;
#endif

            // Initialize Components
            _ = FontTool.Instance;
            ConcurrentTaskDispatcher.Start();

#if EXILED
            base.OnEnabled();
#endif
        }

#if EXILED
        public override void OnDisabled()
#else
        public override void Disable()
#endif
        {
#if EXILED
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.Destroying -= OnDestroying;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
#else
            PlayerEvents.Left -= OnLeft;
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
#endif

            Patcher.Unpatch();
            ClearRoundState();
            ConcurrentTaskDispatcher.Shutdown();

#if EXILED
            base.OnDisabled();
#endif
        }

        private static void OnWaitingForPlayers()
        {
            ClearRoundState();
            ConcurrentTaskDispatcher.Restart();
            Patcher.Patch();
        }

        private static void ClearRoundState()
        {
            // CommonHint owns three periodic schedulers per UI, so tear UIs down before displays.
            PlayerUI.ClearInstance();
            PlayerDisplay.ClearInstance();
        }

        /// <summary>
        /// Releases everything bound to a hub. Safe to call twice; the second call finds nothing.
        /// </summary>
        private static void Destruct(ReferenceHub? referenceHub)
        {
            if (referenceHub is null)
                return;

            PlayerUI.Destruct(referenceHub);
            PlayerDisplay.Destruct(referenceHub);
        }

#if EXILED
        private static void OnDestroying(Exiled.Events.EventArgs.Player.DestroyingEventArgs ev)
        {
            Destruct(ev.Player?.ReferenceHub);
        }

        private static void OnLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
#else
        private void OnLeft(PlayerLeftEventArgs ev)
#endif
        {
            Destruct(ev.Player?.ReferenceHub);
        }
    }
}
