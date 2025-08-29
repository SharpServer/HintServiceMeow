namespace HintServiceMeow.Core.Models.Hints
{
    using HintServiceMeow.Core.Enum;

    public class DynamicHint : AbstractHint
    {
        private float topBoundary = 0;
        private float bottomBoundary = 1000;

        private float leftBoundary = -1200;
        private float rightBoundary = 1200;

        private float targetY = 700;
        private float targetX = 0;

        private float topMargin = 5;
        private float bottomMargin = 5;
        private float leftMargin = 100;
        private float rightMargin = 100;

        private HintPriority priority = HintPriority.Medium;
        private DynamicHintStrategy strategy = DynamicHintStrategy.Hide;

        #region Constructors

        public DynamicHint()
        {
        }

        public DynamicHint(DynamicHint hint)
            : base(hint)
        {
            Lock.EnterWriteLock();
            try
            {
                topBoundary = hint.topBoundary;
                bottomBoundary = hint.bottomBoundary;

                leftBoundary = hint.leftBoundary;
                rightBoundary = hint.rightBoundary;

                targetY = hint.targetY;
                targetX = hint.targetX;

                topMargin = hint.topMargin;
                bottomMargin = hint.bottomMargin;
                leftMargin = hint.leftMargin;
                rightMargin = hint.rightMargin;

                priority = hint.priority;
                strategy = hint.strategy;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the top boundary of the dynamic hint.
        /// </summary>
        public float TopBoundary
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return topBoundary;
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
                    if (topBoundary.Equals(value))
                        return;

                    topBoundary = value;
                    OnHintUpdated("TopBoundary");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the bottom boundary of the dynamic hint.
        /// </summary>
        public float BottomBoundary
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return bottomBoundary;
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
                    if (bottomBoundary.Equals(value))
                        return;

                    bottomBoundary = value;
                    OnHintUpdated("BottomBoundary");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the left boundary of the dynamic hint. Should be more than -1200.
        /// </summary>
        public float LeftBoundary
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return leftBoundary;
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
                    if (leftBoundary.Equals(value))
                        return;

                    leftBoundary = value;
                    OnHintUpdated("LeftBoundary");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the right boundary of the dynamic hint. Should be less than 1200.
        /// </summary>
        public float RightBoundary
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return rightBoundary;
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
                    if (rightBoundary.Equals(value))
                        return;

                    rightBoundary = value;
                    OnHintUpdated("RightBoundary");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Y coordinate that dynamic hint will try to reach.
        /// </summary>
        public float TargetY
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return targetY;
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
                    if (targetY.Equals(value))
                        return;

                    targetY = value;
                    OnHintUpdated("TargetY");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the X coordinate that dynamic hint will try to reach.
        /// </summary>
        public float TargetX
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return targetX;
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
                    if (targetX.Equals(value))
                        return;

                    targetX = value;
                    OnHintUpdated("TargetX");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        public float TopMargin
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return topMargin;
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
                    if (topMargin.Equals(value))
                        return;

                    topMargin = value;
                    OnHintUpdated("TopMargin");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        public float BottomMargin
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return bottomMargin;
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
                    if (bottomMargin.Equals(value))
                        return;

                    bottomMargin = value;
                    OnHintUpdated("BottomMargin");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        public float LeftMargin
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return leftMargin;
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
                    if (leftMargin.Equals(value))
                        return;

                    leftMargin = value;
                    OnHintUpdated("LeftMargin");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        public float RightMargin
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return rightMargin;
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
                    if (rightMargin.Equals(value))
                        return;

                    rightMargin = value;
                    OnHintUpdated("RightMargin");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the priority of the hint, higher priority means the hint is less likely to be covered by other hint.
        /// </summary>
        public HintPriority Priority
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return priority;
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
                    if (priority == value)
                        return;

                    priority = value;
                    OnHintUpdated("Priority");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }

        public DynamicHintStrategy Strategy
        {
            get
            {
                Lock.EnterReadLock();
                try
                {
                    return strategy;
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
                    if (strategy == value)
                        return;

                    strategy = value;
                    OnHintUpdated("Strategy");
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
        }
    }
}
