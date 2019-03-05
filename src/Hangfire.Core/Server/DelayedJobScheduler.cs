// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Server
{
    /// <summary>
    /// 延迟作业调度器
    /// Represents a background process responsible for <i>enqueueing delayed jobs</i>.
    /// 表示负责对延迟作业排队的后台进程。
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This background process polls the <i>delayed job schedule</i> for delayed jobs that are ready to be enqueued. 
    /// 这个后台进程为准备排队的延迟作业轮询延迟作业调度。
    /// To prevent a stress load on a job storage, the configurable delay is used between scheduler runs. 
    /// 为了防止作业存储上的压力负载，在调度程序运行之间使用可配置的延迟。
    /// Delay is used only when there are no more background jobs to be enqueued.
    /// 延迟仅在没有更多后台作业排队时使用。
    /// </para>
    /// 
    /// <para>
    /// When a background job is ready to be enqueued, it is simply moved from <see cref="ScheduledState"/> to the <see cref="EnqueuedState"/> by using <see cref="IBackgroundJobStateChanger"/>.
    /// 当后台作业准备进入队列时，只需使用IBackgroundJobStateChanger将其从ScheduledState移动到EnqueuedState。
    /// </para>
    /// 
    /// <para>
    /// Delayed job schedule is based on a Set data structure of a job storage, so you can use this background process as an example of a custom extension.
    /// 延迟作业调度基于作业存储的一组数据结构，因此可以使用此后台流程作为自定义扩展的示例。
    /// </para>
    ///  
    /// <para>
    /// Multiple instances of this background process can be used in separate threads/processes without additional configuration (distributed locks are used). 
    /// 这个后台进程的多个实例可以在单独的线程/进程中使用，而不需要额外的配置(使用分布式锁)。
    /// However, this only adds support for fail-over, and does not increase the performance.
    /// 但是，这只增加了对故障转移的支持，并没有提高性能。
    /// </para>
    /// 
    /// <note type="important">
    /// If you are using <b>custom filter providers</b>, you need to pass a custom <see cref="IBackgroundJobStateChanger"/> instance to make this process respect your filters when enqueueing background jobs.
    /// 如果您正在使用自定义过滤器提供程序，那么您需要传递一个自定义IBackgroundJobStateChanger实例，以使该流程在排队后台作业时遵守您的过滤器。
    /// </note>
    /// </remarks>
    /// 
    /// <threadsafety static="true" instance="true"/>
    /// 
    /// <seealso cref="ScheduledState"/>
    public class DelayedJobScheduler : IBackgroundProcess
    {
        /// <summary>
        /// Represents a default polling interval for delayed job scheduler. 
        /// 表示延迟作业调度程序的默认轮询间隔。15秒。
        /// This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>TimeSpan.FromSeconds(15)</c>.
        /// </remarks>
        public static readonly TimeSpan DefaultPollingDelay = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(1);

        private readonly ILog _logger = LogProvider.For<DelayedJobScheduler>();

        /// <summary>
        /// 状态改变器
        /// </summary>
        private readonly IBackgroundJobStateChanger _stateChanger;
        private readonly TimeSpan _pollingDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedJobScheduler"/> class with the <see cref="DefaultPollingDelay"/> value as a delay between runs.
        /// 初始化DelayedJobScheduler类的新实例，使用DefaultPollingDelay值作为运行之间的延迟。
        /// </summary>
        public DelayedJobScheduler() 
            : this(DefaultPollingDelay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedJobScheduler"/> class with a specified polling interval.
        /// 使用指定的轮询间隔初始化DelayedJobScheduler类的新实例。
        /// </summary>
        /// <param name="pollingDelay">
        /// Delay between scheduler runs.
        /// 调度程序运行之间的延迟。
        /// </param>
        public DelayedJobScheduler(TimeSpan pollingDelay)
            : this(pollingDelay, new BackgroundJobStateChanger())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedJobScheduler"/>
        /// class with a specified polling interval and given state changer.
        /// </summary>
        /// <param name="pollingDelay">Delay between scheduler runs.</param>
        /// <param name="stateChanger">State changer to use for background jobs.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="stateChanger"/> is null.</exception>
        public DelayedJobScheduler(TimeSpan pollingDelay, [NotNull] IBackgroundJobStateChanger stateChanger)
        {
            if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));

            _stateChanger = stateChanger;
            _pollingDelay = pollingDelay;
        }

        /// <inheritdoc />
        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var jobsEnqueued = 0;

            while (EnqueueNextScheduledJob(context))
            {
                jobsEnqueued++;

                if (context.IsShutdownRequested)
                {
                    break;
                }
            }

            if (jobsEnqueued != 0)
            {
                _logger.Info($"{jobsEnqueued} scheduled job(s) enqueued.");
            }

            context.Wait(_pollingDelay);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetType().Name;
        }

        private bool EnqueueNextScheduledJob(BackgroundProcessContext context)
        {
            return UseConnectionDistributedLock(context.Storage, connection =>
            {
                var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);

                // TODO: it is very slow. Add batching.
                var jobId = connection.GetFirstByLowestScoreFromSet("schedule", 0, timestamp);

                if (jobId == null)
                {
                    // No more scheduled jobs pending.
                    return false;
                }
                
                var appliedState = _stateChanger.ChangeState(new StateChangeContext(
                    context.Storage,
                    connection,
                    jobId,
                    new EnqueuedState { Reason = $"Triggered by {ToString()}" }, 
                    ScheduledState.StateName));

                if (appliedState == null)
                {
                    // When a background job with the given id does not exist, we should
                    // remove its id from a schedule manually. This may happen when someone
                    // modifies a storage bypassing Hangfire API.
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        transaction.RemoveFromSet("schedule", jobId);
                        transaction.Commit();
                    }
                }

                return true;
            });
        }

        private T UseConnectionDistributedLock<T>(JobStorage storage, Func<IStorageConnection, T> action)
        {
            var resource = "locks:schedulepoller";
            try
            {
                using (var connection = storage.GetConnection())
                using (connection.AcquireDistributedLock(resource, DefaultLockTimeout))
                {
                    return action(connection);
                }
            }
            catch (DistributedLockTimeoutException e) when (e.Resource == resource)
            {
                // DistributedLockTimeoutException here doesn't mean that delayed jobs weren't enqueued.
                // It just means another Hangfire server did this work.
                _logger.DebugException(
                    $@"An exception was thrown during acquiring distributed lock on the {resource} resource within {DefaultLockTimeout.TotalSeconds} seconds. The scheduled jobs have not been handled this time.
It will be retried in {_pollingDelay.TotalSeconds} seconds", 
                    e);
                return default(T);
            }
        }
    }
}