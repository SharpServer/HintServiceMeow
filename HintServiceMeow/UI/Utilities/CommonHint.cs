namespace HintServiceMeow.UI.Utilities
{
    using System;
    using System.Collections.Generic;
    using HintServiceMeow.Core.Enum;
    using HintServiceMeow.Core.Extension;
    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities;
    using HintServiceMeow.Plugin;

    public class CommonHint : Core.Interface.IDestructible
    {
        private const string HintGroupId = "HSM_CommonHint";

        #region Common Hints
        private readonly TaskScheduler itemHintsHideScheduler;

        private readonly List<Hint> itemHints =
        [
            new()
            {
                FontSize = 25,
            },

            new()
            {
                YCoordinate = 725,
                FontSize = 25,
            },
        ];

        private readonly TaskScheduler mapHintsHideScheduler;
        private readonly List<Hint> mapHints =
        [
            new()
            {
                YCoordinate = 200,
                FontSize = 25,
            },

            new()
            {
                YCoordinate = 225,
                FontSize = 25,
            },
        ];

        private readonly TaskScheduler roleHintsHideScheduler;
        private readonly List<Hint> roleHints =
        [
            new()
            {
                YCoordinate = 100,
                FontSize = 30,
                Alignment = HintAlignment.Left,
            },

            new()
            {
                YCoordinate = 130,
                FontSize = 25,
                Alignment = HintAlignment.Left,
            },

            new()
            {
                YCoordinate = 155,
                FontSize = 25,
                Alignment = HintAlignment.Left,
            },

            new()
            {
                YCoordinate = 180,
                FontSize = 25,
                Alignment = HintAlignment.Left,
            },
        ];
        #endregion

        #region Constructor

        internal CommonHint(ReferenceHub referenceHub)
        {
            ReferenceHub = referenceHub;

            itemHintsHideScheduler = new TaskScheduler();
            mapHintsHideScheduler = new TaskScheduler();
            roleHintsHideScheduler = new TaskScheduler();

            itemHintsHideScheduler.Start(TimeSpan.Zero, () => itemHints.ForEach(x => x.Hide = true));
            mapHintsHideScheduler.Start(TimeSpan.Zero, () => mapHints.ForEach(x => x.Hide = true));
            roleHintsHideScheduler.Start(TimeSpan.Zero, () => roleHints.ForEach(x => x.Hide = true));

            // Add hint
            foreach (Hint itemHint in itemHints)
                PlayerDisplay.InternalAddHint(HintGroupId, itemHint);
            foreach (Hint mapHint in mapHints)
                PlayerDisplay.InternalAddHint(HintGroupId, mapHint);
            foreach (Hint roleHint in roleHints)
                PlayerDisplay.InternalAddHint(HintGroupId, roleHint);
        }
        #endregion

        #region Properties
        private static PluginConfig Config => PluginConfig.Instance;

        private ReferenceHub ReferenceHub { get; }

        private PlayerDisplay PlayerDisplay => PlayerDisplay.Get(ReferenceHub);
        #endregion

        void Core.Interface.IDestructible.Destruct()
        {
            PlayerDisplay.InternalClearHint(HintGroupId);
        }

        #region Common Hint Methods

        #region Common Item Hints Methods
        public void ShowItemHint(string itemName) => ShowItemHint(itemName, Config.ShortItemHintDisplayTime);

        public void ShowItemHint(string itemName, float time) => ShowItemHint(itemName, [], time);

        public void ShowItemHint(string itemName, string description) => ShowItemHint(itemName, [description], Config.ItemHintDisplayTime);

        public void ShowItemHint(string itemName, string description, float time) => ShowItemHint(itemName, [description], time);

        public void ShowItemHint(string itemName, string[] description) => ShowItemHint(itemName, description, Config.ItemHintDisplayTime);

        public void ShowItemHint(string itemName, string[] description, float time)
        {
            itemHintsHideScheduler.Invoke(time, DelayType.Override);

            itemHints[0].Text = itemName;
            itemHints[0].Hide = false;

            for (int i = 1; i < itemHints.Count; i++)
            {
                if (!description.TryGet(i - 1, out string element))
                    break;

                itemHints[i].Text = element;
                itemHints[i].Hide = false;
            }
        }
        #endregion Common Item Hints Methods

        #region Common Map Hints Methods
        public void ShowMapHint(string roomName) => ShowMapHint(roomName, Config.ShortMapHintDisplayTime);

        public void ShowMapHint(string roomName, float time) => ShowMapHint(roomName, [], time);

        public void ShowMapHint(string roomName, string description) => ShowMapHint(roomName, [description], Config.MapHintDisplayTime);

        public void ShowMapHint(string roomName, string description, float time) => ShowMapHint(roomName, [description], time);

        public void ShowMapHint(string roomName, string[] description) => ShowMapHint(roomName, description, Config.MapHintDisplayTime);

        public void ShowMapHint(string roomName, string[] description, float time)
        {
            mapHintsHideScheduler.Invoke(time, DelayType.Override);

            mapHints.ForEach(x => x.Hide = true);

            mapHints[0].Text = roomName;
            mapHints[0].Hide = false;

            for (int i = 1; i < mapHints.Count; i++)
            {
                if (!description.TryGet(i - 1, out string element))
                    break;

                mapHints[i].Text = element;
                mapHints[i].Hide = false;
            }
        }
        #endregion Common Map Hints Methods

        #region Common Role Hints Methods
        public void ShowRoleHint(string roleName) => ShowRoleHint(roleName, Config.ShortRoleHintDisplayTime);

        public void ShowRoleHint(string roleName, float time) => ShowRoleHint(roleName, [], time);

        public void ShowRoleHint(string roleName, string description) => ShowRoleHint(roleName, [description], Config.RoleHintDisplayTime);

        public void ShowRoleHint(string roleName, string description, float time) => ShowRoleHint(roleName, [description], time);

        public void ShowRoleHint(string roleName, string[] description) => ShowRoleHint(roleName, description, Config.RoleHintDisplayTime);

        public void ShowRoleHint(string roleName, string[] description, float time)
        {
            roleHintsHideScheduler.Invoke(time, DelayType.Override);

            roleHints.ForEach(x => x.Hide = true);

            roleHints[0].Text = roleName;
            roleHints[0].Hide = false;

            for (int i = 1; i < roleHints.Count; i++)
            {
                if (!description.TryGet(i - 1, out string element))
                    break;

                roleHints[i].Text = element;
                roleHints[i].Hide = false;
            }
        }
        #endregion Common Role Hints Methods

        #region Common Other Hints Methods
        public void ShowOtherHint(string messages) => ShowOtherHint(messages, Config.OtherHintDisplayTime);

        public void ShowOtherHint(string messages, float time) => ShowOtherHint([messages], time);

        public void ShowOtherHint(string[] messages) => ShowOtherHint(messages, Config.OtherHintDisplayTime * messages.Length);

        public void ShowOtherHint(string[] messages, float time)
        {
            foreach (string message in messages)
            {
                DynamicHint dynamicHint = new()
                {
                    Text = message,
                    TopBoundary = 400,
                    BottomBoundary = 1000,
                    TargetY = 700,
                };

                PlayerDisplay.InternalAddHint("Other Hint", dynamicHint);
                PlayerDisplay.RemoveAfter(dynamicHint, time);
            }
        }
        #endregion Common Other Hints Methods

        #endregion Common Hint Methods
    }
}
