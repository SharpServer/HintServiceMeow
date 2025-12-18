namespace HintServiceMeow.Core.Models.Hints
{
    using System;
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
                    OnHintUpdated("SyncSpeed");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
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
                    OnHintUpdated("FontSize");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
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
                    OnHintUpdated("LineHeight");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
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

                    content = value;
                    content.ContentUpdated += () => OnHintUpdated("Content");
                    OnHintUpdated("Content");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
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
                        Content = new StringContent(value);
                    }

                    OnHintUpdated("Text");
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
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
                    Content = new AutoContent(value);
                    OnHintUpdated("AutoText");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
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
                    OnHintUpdated("Hide");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        protected ReaderWriterLockSlim Lock { get; } = new(LockRecursionPolicy.SupportsRecursion);
        #endregion

        #region Methods

        public virtual void TryUpdateHint(UpdateAvailableEventArg ev)
        {
            Content.TryUpdate(new ContentUpdateArg(this, ev.PlayerDisplay));
        }

        protected virtual void OnHintUpdated(string argumentName)
        {
            analyser.OnUpdate();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(argumentName));
        }

        #endregion
    }
}
