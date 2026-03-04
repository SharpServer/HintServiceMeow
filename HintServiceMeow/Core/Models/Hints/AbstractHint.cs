namespace HintServiceMeow.Core.Models.Hints
{
    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using System.Threading;
    using HintServiceMeow.Core.Enum;
    using HintServiceMeow.Core.Interface;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Models.HintContent;
    using HintServiceMeow.Core.Utilities;
    using HintServiceMeow.Core.Utilities.Tools;

    public abstract class AbstractHint : INotifyPropertyChanged
    {
        private readonly Guid guid = Guid.NewGuid();

        private IUpdateAnalyser analyser = new UpdateAnalyzer();

        private string id = string.Empty;

        private HintSyncSpeed syncSpeed = HintSyncSpeed.Normal;

        private int fontSize = 20;

        private float lineHeight;

        private AbstractHintContent content = new StringContent(string.Empty);

        private bool hide;

        #region Constructors

        protected AbstractHint()
        {
        }

        protected AbstractHint(AbstractHint hint)
        {
            Lock.EnterWriteLock();
            try
            {
                id = hint.id;
                syncSpeed = hint.syncSpeed;
                fontSize = hint.fontSize;
                lineHeight = hint.lineHeight;
                content = hint.content;
                hide = hint.hide;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Properties
        public IUpdateAnalyser UpdateAnalyser
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return analyser;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    analyser = value;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        public Guid Guid
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return guid;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }
        }

        public string Id
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return id;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    id = value;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        public HintSyncSpeed SyncSpeed
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return syncSpeed;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    if (syncSpeed == value)
                        return;

                    syncSpeed = value;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                OnHintUpdated(nameof(SyncSpeed));
            }
        }

        public int FontSize
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return fontSize;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    if (fontSize == value)
                        return;

                    fontSize = value;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                OnHintUpdated(nameof(FontSize));
            }
        }

        public float LineHeight
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return lineHeight;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    if (lineHeight.Equals(value))
                        return;

                    lineHeight = value;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                OnHintUpdated(nameof(LineHeight));
            }
        }

        public AbstractHintContent Content
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return content;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    if (content == value)
                        return;

                    Content.ContentUpdated -= OnContentUpdate;

                    content = value;
                    content.ContentUpdated += OnContentUpdate;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                OnHintUpdated(nameof(Content));
            }
        }

        public string? Text
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    if (Content is StringContent)
                    {
                        return Content.GetText();
                    }

                    return null;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    if (Content is StringContent textContent)
                    {
                        textContent.Text = value;
                    }
                    else
                    {
                        content.ContentUpdated -= OnContentUpdate;
                        content = new StringContent(value);
                        content.ContentUpdated += OnContentUpdate;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                OnHintUpdated(nameof(Text));
            }
        }

        public AutoContent.TextUpdateHandler? AutoText
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    if (Content is AutoContent autoContent)
                    {
                        return autoContent.AutoText;
                    }

                    return null;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    content.ContentUpdated -= OnContentUpdate;
                    content = new AutoContent(value);
                    content.ContentUpdated += OnContentUpdate;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                OnHintUpdated(nameof(AutoText));
            }
        }

        public bool Hide
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return hide;
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            set
            {
                Lock.EnterWriteLock();
                try
                {
                    if (hide == value)
                        return;

                    hide = value;
                }
                finally
                {
                    Lock.ExitWriteLock();
                }

                OnHintUpdated(nameof(Hide));
            }
        }

        internal ConcurrentDictionary<object, object> InternalCache { get; set; } = new ConcurrentDictionary<object, object>();

        protected ReaderWriterLockSlim Lock { get; } = new(LockRecursionPolicy.SupportsRecursion);
        #endregion

        #region Methods

        public virtual void TryUpdateHint(UpdateAvailableEventArg ev)
        {
            Content.TryUpdate(new ContentUpdateArg(this, ev.PlayerDisplay));
        }

        /// <summary>
        /// Not thread friendly, should only be used in pool.
        /// </summary>
        /// <param name="copyFrom">Copy parameter from.</param>
        internal void CopyFieldsFrom(AbstractHint copyFrom)
        {
            this.id = copyFrom.id;
            this.syncSpeed = copyFrom.syncSpeed;
            this.fontSize = copyFrom.fontSize;
            this.lineHeight = copyFrom.lineHeight;
            this.content = copyFrom.content;
            this.hide = copyFrom.hide;
        }

        /// <summary>
        /// Not thread friendly, should only be used in pool.
        /// </summary>
        internal void ResetFields()
        {
            this.id = string.Empty;
            this.content = null!;
        }

        protected virtual void OnHintUpdated(string argumentName)
        {
            InternalCache.Clear();
            analyser.OnUpdate();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(argumentName));
        }

        private void OnContentUpdate()
        {
            OnHintUpdated(nameof(Content));
        }
        #endregion
    }
}
