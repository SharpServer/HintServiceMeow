namespace HintServiceMeow.Core.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using HintServiceMeow.Core.Enum;
    using HintServiceMeow.Core.Interface;
    using HintServiceMeow.Core.Models;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities.Parser;
    using HintServiceMeow.Core.Utilities.Tools;

    using MEC;

    /// <summary>
    /// Represent a player's display. This class is used to manage hints and update hint to player's display.
    /// </summary>
    public class PlayerDisplay : IPlayerDisplay, IDestructible
    {
        private static readonly HashSet<PlayerDisplay> PlayerDisplayList = [];
        private static readonly object PlayerDisplayListLock = new();

        private readonly List<IDisplayOutput> displayOutputs = [];

        private readonly IPlayerContext playerContext;
        private readonly HintCollection displayHints = new();
        private readonly ITaskScheduler updateScheduler; // Initialize in constructor

        private readonly object displayOutputsLock = new();
        private readonly object currentParserTaskLock = new();

        private IHintParser hintParser = new HintParser();
        private ICompatibilityAdaptor adapter; // Initialize in constructor

        private CoroutineHandle coroutine; // Initialize in constructor

        private Task? currentParserTask;

        internal PlayerDisplay(
            IPlayerContext playerContext,
            HintCollection? displayHints = null,
            ITaskScheduler? updateScheduler = null,
            ICompatibilityAdaptor? adaptor = null,
            IHintParser? hintParser = null,
            IEnumerable<IDisplayOutput>? displayOutputs = null)
        {
            // Initialize each components
            this.playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));

            if (displayHints != null)
                this.displayHints = displayHints;
            if (hintParser != null)
                this.hintParser = hintParser;
            if (displayOutputs != null)
                this.displayOutputs = displayOutputs.ToList();

            adapter = adaptor ?? new CompatibilityAdaptor(this); // Default compatibility adaptor
            this.updateScheduler = updateScheduler ?? new TaskScheduler(); // Default task scheduler with zero interval

            // When collection changed, update the content on player's screen
            this.displayHints.CollectionChanged += OnCollectionChanged;

            // Initialize update scheduler. Set action of the scheduler to start parser task.
            this.updateScheduler.Start(TimeSpan.Zero, () =>
            {
                this.updateScheduler.Pause(); // Pause action until the parser task is finishing
                StartParserTask();
            });

            // Start the main coroutine on main thread
            MainThreadDispatcher.Dispatch(() => coroutine = Timing.RunCoroutine(CoroutineMethod()));
        }

        private PlayerDisplay(ReferenceHub referenceHub)
            : this(new ReferenceHubContext(referenceHub))
        {
            if(referenceHub is null)
                throw new ArgumentNullException(nameof(referenceHub));

            // Check if this belongs to local player (npc)
            if (referenceHub.isServer)
                return;

            displayOutputs.Add(new DefaultDisplayOutput(referenceHub.connectionToClient));
        }

        public delegate void UpdateAvailableEventHandler(UpdateAvailableEventArg ev);

        /// <summary>
        /// Invoke every tick when ReferenceHub display is ready to update.
        /// </summary>
        public event UpdateAvailableEventHandler? UpdateAvailable;

        /// <summary>
        /// Gets the player this instance binds to.
        /// </summary>
        public ReferenceHub? ReferenceHub => playerContext is ReferenceHubContext context ? context.ReferenceHub : throw new NullReferenceException();

        public IHintParser HintParser
        {
            get => hintParser;
            set => hintParser = value ?? throw new ArgumentNullException(nameof(value));
        }

        public ICompatibilityAdaptor CompatibilityAdaptor
        {
            get => adapter;
            set => adapter = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Get the PlayerDisplay instance of the player. If the instance have not been created yet, then it will create one.
        /// Not Thread Safe.
        /// </summary>
        /// <param name="referenceHub">The <see cref="global::ReferenceHub"/> that owns the <see cref="PlayerDisplay"/>.</param>
        /// <returns>The PlayerDisplay assigned to the given <see cref="global::ReferenceHub"/>.</returns>
        public static PlayerDisplay Get(ReferenceHub referenceHub)
        {
            if (referenceHub is null)
                throw new ArgumentNullException(nameof(referenceHub));

            ReferenceHubContext context = new(referenceHub);

            lock (PlayerDisplayListLock)
            {
                PlayerDisplay? existing = PlayerDisplayList.FirstOrDefault(x => x.playerContext.Equals(context));

                if (existing is not null)
                    return existing;

                PlayerDisplay newPlayerDisplay = new(referenceHub);
                PlayerDisplayList.Add(newPlayerDisplay);
                return newPlayerDisplay;
            }
        }

        /// <summary>
        /// Get the PlayerDisplay instance of the player. If the instance have not been created yet, then it will create one.
        /// Not Thread Safe.
        /// </summary>
        /// <param name="player">The owner of the <see cref="PlayerDisplay"/>.</param>
        /// <returns>The PlayerDisplay assigned to the given <see cref="LabApi.Features.Wrappers.Player"/>.</returns>
        public static PlayerDisplay Get(LabApi.Features.Wrappers.Player player)
        {
            if (player is null)
                throw new ArgumentNullException(nameof(player));

            return Get(player.ReferenceHub);
        }

        #if EXILED
        /// <summary>
        /// Get the PlayerDisplay instance of the player. If the instance have not been created yet, then it will create one.
        /// Not Thread Safe.
        /// </summary>
        /// <param name="player">The owner of the <see cref="PlayerDisplay"/>.</param>
        /// <returns>The players <see cref="PlayerDisplay"/>.</returns>
        public static PlayerDisplay Get(Exiled.API.Features.Player player)
        {
            if (player is null)
                throw new ArgumentNullException(nameof(player));

            return Get(player.ReferenceHub);
        }
        #endif

        /// <summary>
        /// Force an update when the update is available. You do not have to use this method unless you are using HintSyncSpeed.UnSync.
        /// </summary>
        /// <param name="useFastUpdate">Forces next update as soon as possible.</param>
        public void ForceUpdate(bool useFastUpdate = false)
        {
            ScheduleUpdate(useFastUpdate ? 0f : 0.3f);
        }

        public void AddDisplayOutput(IDisplayOutput output)
        {
            lock (displayOutputsLock)
            {
                displayOutputs.Add(output);
            }
        }

        public void RemoveDisplayOutput(IDisplayOutput output)
        {
            lock (displayOutputsLock)
            {
                displayOutputs.Remove(output);
            }
        }

        public void RemoveDisplayOutput<T>()
            where T : IDisplayOutput
        {
            lock (displayOutputsLock)
            {
                displayOutputs.RemoveAll(x => x is T);
            }
        }

        public void AddHint(AbstractHint? hint)
        {
            if (hint is null)
                return;

            InternalAddHint(Assembly.GetCallingAssembly().FullName, hint);
        }

        public void AddHint(IEnumerable<AbstractHint>? hints)
        {
            if (hints is null)
                return;

            string groupName = Assembly.GetCallingAssembly().FullName;

            foreach (AbstractHint hint in hints)
            {
                InternalAddHint(groupName, hint);
            }
        }

        public void AddHint(params AbstractHint[]? hints)
        {
            if (hints is null || hints.Length == 0)
                return;

            string groupName = Assembly.GetCallingAssembly().FullName;
            foreach (AbstractHint hint in hints)
            {
                InternalAddHint(groupName, hint);
            }
        }

        public void RemoveHint(AbstractHint? hint)
        {
            if (hint is null)
                return;

            InternalRemoveHint(Assembly.GetCallingAssembly().FullName, hint);
        }

        public void RemoveHint(IEnumerable<AbstractHint>? hints)
        {
            if (hints is null)
                return;

            string groupName = Assembly.GetCallingAssembly().FullName;
            foreach (AbstractHint hint in hints)
            {
                InternalRemoveHint(groupName, hint);
            }
        }

        public void RemoveHint(params AbstractHint[]? hints)
        {
            if (hints is null || hints.Length == 0)
                return;

            string groupName = Assembly.GetCallingAssembly().FullName;
            foreach (AbstractHint hint in hints)
            {
                InternalRemoveHint(groupName, hint);
            }
        }

        public void RemoveHint(string id)
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id));

            if (id == string.Empty)
                throw new ArgumentException("A empty string had been passed to RemoveHint");

            InternalRemoveHint(Assembly.GetCallingAssembly().FullName, id);
        }

        public void RemoveHint(Guid id)
        {
            InternalRemoveHint(Assembly.GetCallingAssembly().FullName, id);
        }

        public void ClearHint()
        {
            InternalClearHint(Assembly.GetCallingAssembly().FullName);
        }

        /// <summary>
        /// Return the first hint that match the id.
        /// </summary>
        /// <param name="id">The ID of the hint.</param>
        /// <returns>The found hint.</returns>
        public AbstractHint? GetHint(string? id)
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id));

            if (id == string.Empty)
                throw new ArgumentException("A empty string had been passed to GetHint");

            return InternalGetHints(Assembly.GetCallingAssembly().FullName, x => x.Id == id).FirstOrDefault();
        }

        /// <summary>
        /// Return the first hint that match the guid.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> of the hint.</param>
        /// <returns>The found hint.</returns>
        public AbstractHint? GetHint(Guid guid)
            => InternalGetHints(Assembly.GetCallingAssembly().FullName, x => x.Guid == guid).FirstOrDefault();

        public IEnumerable<AbstractHint> GetHints(string id)
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id));

            if (id == string.Empty)
                throw new ArgumentException("A empty string had been passed to GetHints");

            return InternalGetHints(Assembly.GetCallingAssembly().FullName, x => x.Id == id);
        }

        public IEnumerable<AbstractHint> GetHints()
        {
            return InternalGetHints(Assembly.GetCallingAssembly().FullName);
        }

        public bool HasHint(string id)
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id));

            if (id == string.Empty)
                throw new ArgumentException("A empty string had been passed to HasHint");

            return InternalGetHints(Assembly.GetCallingAssembly().FullName, hint => hint.Id == id).Any();
        }

        public bool HasHint(Guid guid)
        {
            return InternalGetHints(Assembly.GetCallingAssembly().FullName, hint => hint.Guid == guid).Any();
        }

        /// <summary>
        /// Return the first hint that match the id.
        /// </summary>
        /// <param name="id">The ID of the hint.</param>
        /// <param name="hint">The found hint.</param>
        /// <returns>Whether hint is null.</returns>
        #nullable disable
        public bool TryGetHint(string id, out AbstractHint hint)
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id));

            if (id == string.Empty)
                throw new ArgumentException("A empty string had been passed to TryGetHint");

            hint = InternalGetHints(Assembly.GetCallingAssembly().FullName, x => x.Id == id).FirstOrDefault();

            return hint != null;
        }

        /// <summary>
        /// Return the first hint that match the guid.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> of the hint.</param>
        /// <param name="hint">The found hint.</param>
        /// <returns>Whether hint is null.</returns>
        public bool TryGetHint(Guid guid, out AbstractHint hint)
        {
            hint = InternalGetHints(Assembly.GetCallingAssembly().FullName, x => x.Guid == guid).FirstOrDefault();
            return hint != null;
        }
        #nullable restore

        public bool TryGetHints(string? id, out IEnumerable<AbstractHint> hints)
        {
            if (id is null)
                throw new ArgumentNullException(nameof(id));

            if (id == string.Empty)
                throw new ArgumentException("A empty string had been passed to TryGetHints");

            hints = InternalGetHints(Assembly.GetCallingAssembly().FullName, x => x.Id == id);
            return hints.Any();
        }

        void IDestructible.Destruct()
        {
            Timing.KillCoroutines(coroutine); // Stop coroutine
            UpdateAvailable = null; // Clear event

            // Clear collection's reference to this pd
            displayHints.CollectionChanged -= OnCollectionChanged;

            // Clear hint's reference to this pd
            foreach (AbstractHint hint in displayHints.GetHints(null))
            {
                hint.PropertyChanged -= OnHintUpdate;
                UpdateAvailable -= hint.TryUpdateHint;
            }

            // Clear pd's reference to hints
            displayHints.ClearHints(null);

            ((IDestructible)updateScheduler).Destruct(); // Stop task scheduler's coroutine

            ((IDestructible)adapter).Destruct(); // Stop compatibility adaptor's coroutine
        }

        /// <summary>
        /// Not thread safe.
        /// </summary>
        /// <param name="referenceHub">The owner of the PlayerDisplay to destroy.</param>
        internal static void Destruct(ReferenceHub referenceHub)
        {
            if (referenceHub is null)
                throw new ArgumentNullException(nameof(referenceHub));

            ReferenceHubContext context = new(referenceHub);

            lock (PlayerDisplayListLock)
            {
                PlayerDisplay? pd = PlayerDisplayList.FirstOrDefault(x => x.playerContext.Equals(context));

                if (pd is null)
                    return;

                ((IDestructible)pd).Destruct();

                PlayerDisplayList.Remove(pd); // Remove from the reference list
            }
        }

        internal void InternalAddHint(string name, AbstractHint hint)
        {
            hint.PropertyChanged += OnHintUpdate;
            UpdateAvailable += hint.TryUpdateHint;

            displayHints.AddHint(name, hint);
        }

        internal void InternalRemoveHint(string? name, AbstractHint hint)
        {
            hint.PropertyChanged -= OnHintUpdate;
            UpdateAvailable -= hint.TryUpdateHint;

            displayHints.RemoveHint(name, hint);
        }

        internal void InternalRemoveHint(string name, Guid guid)
        {
            AbstractHint? hint = displayHints.GetHints(name).FirstOrDefault(x => x.Guid.Equals(guid));

            if (hint == null)
                return;

            hint.PropertyChanged -= OnHintUpdate;
            UpdateAvailable -= hint.TryUpdateHint;

            displayHints.RemoveHint(name, x => x.Guid.Equals(guid));
        }

        internal void InternalRemoveHint(string name, string id)
        {
            IEnumerable<AbstractHint> removeList = displayHints.GetHints(name).Where(predicate => predicate.Id == id);

            foreach (AbstractHint hint in removeList)
            {
                hint.PropertyChanged -= OnHintUpdate;
                UpdateAvailable -= hint.TryUpdateHint;
            }

            displayHints.RemoveHint(name, x => x.Id.Equals(id));
        }

        internal void InternalClearHint(string name)
        {
            foreach (AbstractHint hint in displayHints.GetHints(name).ToList())
            {
                hint.PropertyChanged -= OnHintUpdate;
                UpdateAvailable -= hint.TryUpdateHint;
            }

            displayHints.ClearHints(name);
        }

        internal IReadOnlyList<AbstractHint> InternalGetHints(string name)
        {
            return displayHints.GetHints(name);
        }

        internal IReadOnlyList<AbstractHint> InternalGetHints(string name, Func<AbstractHint, bool> predicate)
        {
            return displayHints.GetHints(name, predicate);
        }

        internal void ShowCompatibilityHint(string assemblyName, string? content, float duration) => adapter.ShowHint(new CompatibilityAdaptorArg(assemblyName, content, duration));

        private IEnumerator<float> CoroutineMethod()
        {
            while (true)
            {
                yield return Timing.WaitForOneFrame;

                // If player has quit, then stop the coroutine
                if (!playerContext.IsValid())
                    break;

                // Reset the success flag
                bool isSuccessful = true;

                try
                {
                    // Periodic update
                    if (updateScheduler.Elapsed > TimeSpan.FromSeconds(5))
                        ScheduleUpdate();

                    if (updateScheduler.IsReadyForNextAction)
                    {
                        UpdateAvailable?.Invoke(new UpdateAvailableEventArg(this));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                    isSuccessful = false; // If error occurred, set the success flag to false
                }

                // If the update is not successful, wait for a while before trying again so that it will not stuck the log.
                if (!isSuccessful)
                {
                    yield return Timing.WaitForSeconds(1f);
                }
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ScheduleUpdate();
        }

        private void OnHintUpdate(object sender, PropertyChangedEventArgs ev)
        {
            if (sender is not AbstractHint hint)
                return;

            // Skip if the hint's property changed when it is hided
            if (ev.PropertyName != "Hide" && hint.Hide)
                return;

            if (hint.SyncSpeed == HintSyncSpeed.UnSync)
                return;

            float maxWaitingTime = hint.SyncSpeed switch
            {
                HintSyncSpeed.Fastest => 0,
                HintSyncSpeed.Fast => 0.1f,
                HintSyncSpeed.Normal => 0.3f,
                HintSyncSpeed.Slow => 1f,
                HintSyncSpeed.Slowest => 3f,
                _ => throw new ArgumentOutOfRangeException()
            };

            ScheduleUpdate(maxWaitingTime, hint);
        }

        private void ScheduleUpdate(float maxWaitingTime = float.MinValue, AbstractHint? updatingHint = null)
        {
            if (maxWaitingTime <= 0)
            {
                updateScheduler.Invoke();
                return;
            }

            IEnumerable<AbstractHint> predictingHints = displayHints.AllGroups.SelectMany(x => x);

            if (updatingHint != null)
            {
                predictingHints = predictingHints.Where(h => h.SyncSpeed >= updatingHint.SyncSpeed && h != updatingHint);
            }

            TimeSpan maxWaitingTimeSpan = TimeSpan.FromSeconds(maxWaitingTime);
            DateTime now = DateTime.Now;

            DateTime delayedUpdateTime = predictingHints
                .Select(h => h.UpdateAnalyser.EstimateNextUpdate())
                .Where(x => x - now >= TimeSpan.Zero && x - now <= maxWaitingTimeSpan)
                .DefaultIfEmpty(now)
                .Max();

            float delay = (float)(delayedUpdateTime - now).TotalSeconds;
            delay = Math.Max(maxWaitingTime, delay * 1.1f); // Increase by 10% to make increase hit rate of prediction

            if (delay <= 0)
                updateScheduler.Invoke();
            else
                updateScheduler.Invoke(delay, DelayType.KeepFastest);
        }

        private void StartParserTask()
        {
            lock (currentParserTaskLock)
            {
                if (currentParserTask is not null)
                    return;

                currentParserTask =
                    ConcurrentTaskDispatcher.Instance.Enqueue(() =>
                    {
                        string richText;

                        try
                        {
                            richText = hintParser.ParseToMessage(displayHints);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error(ex);
                            return Task.FromResult(Task.CompletedTask);
                        }

                        MainThreadDispatcher.Dispatch(() =>
                        {
                            try
                            {
                                SendHint(richText);
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Error(ex);
                            }
                            finally
                            {
                                updateScheduler.Resume(); // Resume action after the parser task is finishing

                                lock (currentParserTaskLock)
                                {
                                    currentParserTask = null;
                                }
                            }
                        });

                        return Task.FromResult(Task.CompletedTask);
                    });
            }
        }

        private void SendHint(string text)
        {
            lock (displayOutputsLock)
            {
                foreach (IDisplayOutput output in displayOutputs.ToArray())
                {
                    try
                    {
                        output.ShowHint(new DisplayOutputArg(this, text));
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            }
        }
    }
}