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
using System.Collections.Generic;
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using System.ComponentModel;

namespace Hangfire
{
    /// <summary>
    /// 后台作业服务
    /// </summary>
    public class BackgroundJobServer : IDisposable
    {
        private readonly ILog _logger = LogProvider.For<BackgroundJobServer>();

        private readonly BackgroundJobServerOptions _options;
        private readonly BackgroundProcessingServer _processingServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class with default options and <see cref="JobStorage.Current"/> storage.
        /// 初始化一个后台作业服务的新实例
        /// </summary>
        public BackgroundJobServer()
            : this(new BackgroundJobServerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class with default options and the given storage.
        /// 初始化一个后台作业服务的新实例
        /// </summary>
        /// <param name="storage">The storage</param>
        public BackgroundJobServer([NotNull] JobStorage storage)
            : this(new BackgroundJobServerOptions(), storage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class with the given options and <see cref="JobStorage.Current"/> storage.
        /// 初始化一个后台作业服务的新实例
        /// </summary>
        /// <param name="options">Server options</param>
        public BackgroundJobServer([NotNull] BackgroundJobServerOptions options)
            : this(options, JobStorage.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobServer"/> class with the specified options and the given storage.
        /// 初始化一个后台作业服务的新实例
        /// </summary>
        /// <param name="options">Server options</param>
        /// <param name="storage">The storage</param>
        public BackgroundJobServer([NotNull] BackgroundJobServerOptions options, [NotNull] JobStorage storage)
            : this(options, storage, Enumerable.Empty<IBackgroundProcess>())
        {
        }

        /// <summary>
        /// 初始化一个后台作业服务的新实例
        /// </summary>
        /// <param name="options"></param>
        /// <param name="storage"></param>
        /// <param name="additionalProcesses"></param>
        public BackgroundJobServer(
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses)
            : this(options, storage, additionalProcesses, 
                   options.FilterProvider ?? JobFilterProviders.Providers,
                   options.Activator ?? JobActivator.Current, 
                   null, null, null)
        {
        }

        /// <summary>
        /// 初始化一个后台作业服务的新实例
        /// </summary>
        /// <param name="options"></param>
        /// <param name="storage"></param>
        /// <param name="additionalProcesses"></param>
        /// <param name="filterProvider"></param>
        /// <param name="activator"></param>
        /// <param name="factory"></param>
        /// <param name="performer"></param>
        /// <param name="stateChanger"></param>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public BackgroundJobServer(
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [NotNull] IJobFilterProvider filterProvider,
            [NotNull] JobActivator activator,
            [CanBeNull] IBackgroundJobFactory factory,
            [CanBeNull] IBackgroundJobPerformer performer,
            [CanBeNull] IBackgroundJobStateChanger stateChanger)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (additionalProcesses == null) throw new ArgumentNullException(nameof(additionalProcesses));
            if (filterProvider == null) throw new ArgumentNullException(nameof(filterProvider));
            if (activator == null) throw new ArgumentNullException(nameof(activator));

            _options = options;

            var processes = new List<IBackgroundProcess>();
            processes.AddRange(GetRequiredProcesses(filterProvider, activator, factory, performer, stateChanger));
            processes.AddRange(additionalProcesses);

            var properties = new Dictionary<string, object>
            {
                { "Queues", options.Queues },
                { "WorkerCount", options.WorkerCount }
            };

            //_logger.Info("Starting Hangfire Server");
            _logger.Info("启动Hangfire服务器");
            //_logger.Info($"Using job storage: '{storage}'");
            _logger.Info($"使用的作业存储: '{storage}'");

            storage.WriteOptionsToLog(_logger);

            //_logger.Info("Using the following options for Hangfire Server:");
            _logger.Info("为Hangfire服务器使用以下选项:");
            //_logger.Info($"    Worker count: {options.WorkerCount}");
            _logger.Info($"    工作者数量: {options.WorkerCount}");
            //_logger.Info($"    Listening queues: {String.Join(", ", options.Queues.Select(x => "'" + x + "'"))}");
            _logger.Info($"    监听队列: {String.Join(", ", options.Queues.Select(x => "'" + x + "'"))}");
            //_logger.Info($"    Shutdown timeout: {options.ShutdownTimeout}");
            _logger.Info($"    关闭超时: {options.ShutdownTimeout}");
            //_logger.Info($"    Schedule polling interval: {options.SchedulePollingInterval}");
            _logger.Info($"    计划轮询间隔: {options.SchedulePollingInterval}");

            _processingServer = new BackgroundProcessingServer(
                storage, 
                processes, 
                properties, 
                GetProcessingServerOptions());
        }

        public void SendStop()
        {
            _logger.Debug("Hangfire Server is stopping...");
            _processingServer.SendStop();
        }

        public void Dispose()
        {
            _processingServer.Dispose();
            _logger.Info("Hangfire Server stopped.");
        }

        private IEnumerable<IBackgroundProcess> GetRequiredProcesses(
            [NotNull] IJobFilterProvider filterProvider,
            [NotNull] JobActivator activator,
            [CanBeNull] IBackgroundJobFactory factory,
            [CanBeNull] IBackgroundJobPerformer performer,
            [CanBeNull] IBackgroundJobStateChanger stateChanger)
        {
            var processes = new List<IBackgroundProcess>();
            
            factory = factory ?? new BackgroundJobFactory(filterProvider);
            performer = performer ?? new BackgroundJobPerformer(filterProvider, activator);
            stateChanger = stateChanger ?? new BackgroundJobStateChanger(filterProvider);
            
            for (var i = 0; i < _options.WorkerCount; i++)
            {
                processes.Add(new Worker(_options.Queues, performer, stateChanger));
            }
            
            processes.Add(new DelayedJobScheduler(_options.SchedulePollingInterval, stateChanger));
            processes.Add(new RecurringJobScheduler(factory));

            return processes;
        }

        /// <summary>
        /// 获得进程服务选项
        /// </summary>
        /// <returns></returns>
        private BackgroundProcessingServerOptions GetProcessingServerOptions()
        {
            return new BackgroundProcessingServerOptions
            {
                ShutdownTimeout = _options.ShutdownTimeout,
                HeartbeatInterval = _options.HeartbeatInterval,
#pragma warning disable 618
                ServerCheckInterval = _options.ServerWatchdogOptions?.CheckInterval ?? _options.ServerCheckInterval,
                ServerTimeout = _options.ServerWatchdogOptions?.ServerTimeout ?? _options.ServerTimeout,
                ServerName = _options.ServerName
#pragma warning restore 618
            };
        }

        [Obsolete("This method is a stub. There is no need to call the `Start` method. Will be removed in version 2.0.0.")]
        public void Start()
        {
        }

        [Obsolete("This method is a stub. Please call the `Dispose` method instead. Will be removed in version 2.0.0.")]
        public void Stop()
        {
        }
    }
}
