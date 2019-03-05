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
#if NETFULL
using System.Diagnostics;
#endif
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire.Server
{
    /// <summary>
    /// Responsible for running the given collection background processes.
    /// 负责运行给定的后台进程集合。
    /// </summary>
    /// 
    /// <remarks>
    /// Immediately starts the processes in a background thread.
    /// 立即在后台线程中启动进程。
    /// Responsible for announcing/removing a server, bound to a storage.
    /// 负责通知/删除绑定到存储的服务器。
    /// Wraps all the processes with a infinite loop and automatic retry.
    /// 用无限循环和自动重试包装所有进程。
    /// Executes all the processes in a single context.
    /// 在单个上下文中执行所有进程。
    /// Uses timeout in dispose method, waits for all the components, cancel signals shutdown Contains some required processes and uses storage processes.
    /// 在dispose方法中使用超时，等待所有组件，cancel信号关闭包含一些需要的进程，并使用存储进程。
    /// Generates unique id.
    /// 生成惟一的id。
    /// Properties are still bad.
    /// </remarks>
    public sealed class BackgroundProcessingServer : IBackgroundProcess, IDisposable
    {
        /// <summary>
        /// 默认的关闭超时
        /// </summary>
        public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILog _logger = LogProvider.For<BackgroundProcessingServer>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
#pragma warning disable 618
        private readonly List<IServerProcess> _processes = new List<IServerProcess>();
#pragma warning restore 618

        private readonly BackgroundProcessingServerOptions _options;
        /// <summary>
        /// 引导/启动任务
        /// </summary>
        private readonly Task _bootstrapTask;

        public BackgroundProcessingServer([NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(JobStorage.Current, processes)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(JobStorage.Current, processes, properties)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(storage, processes, new Dictionary<string, object>())
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(storage, processes, properties, new BackgroundProcessingServerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundProcessingServer"/> class and immediately starts all the given background processes.
        /// 初始化BackgroundProcessingServer类的新实例，并立即启动所有给定的后台进程。
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="processes"></param>
        /// <param name="properties"></param>
        /// <param name="options"></param>
        public BackgroundProcessingServer(
            [NotNull] JobStorage storage, 
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties, 
            [NotNull] BackgroundProcessingServerOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (processes == null) throw new ArgumentNullException(nameof(processes));
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _options = options;

            _processes.AddRange(GetRequiredProcesses());
            _processes.AddRange(storage.GetComponents());
            _processes.AddRange(processes);

            var context = new BackgroundProcessContext(
                GetGloballyUniqueServerId(), 
                storage,
                properties,
                _cts.Token);

            _bootstrapTask = WrapProcess(this).CreateTask(context);
        }

        public void SendStop()
        {
            _cts.Cancel();
        }

        public void Dispose()
        {
            SendStop();

            // TODO: Dispose _cts

            if (!_bootstrapTask.Wait(_options.ShutdownTimeout))
            {
                _logger.Warn("Processing server takes too long to shutdown. Performing ungraceful shutdown.");
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        void IBackgroundProcess.Execute(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            {
                var serverContext = GetServerContext(context.Properties);
                connection.AnnounceServer(context.ServerId, serverContext);
            }

            try
            {
                var tasks = _processes
                    .Select(WrapProcess)
                    .Select(process => process.CreateTask(context))
                    .ToArray();

                Task.WaitAll(tasks);
            }
            finally
            {
                using (var connection = context.Storage.GetConnection())
                {
                    connection.RemoveServer(context.ServerId);
                }
            }
        }

        private IEnumerable<IBackgroundProcess> GetRequiredProcesses()
        {
            yield return new ServerHeartbeat(_options.HeartbeatInterval);
            yield return new ServerWatchdog(_options.ServerCheckInterval, _options.ServerTimeout);
        }

        private string GetGloballyUniqueServerId()
        {
            var serverName = _options.ServerName
                ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
                ?? Environment.GetEnvironmentVariable("HOSTNAME");

            var guid = Guid.NewGuid().ToString();

#if NETFULL
            if (!String.IsNullOrWhiteSpace(serverName))
            {
                serverName += ":" + Process.GetCurrentProcess().Id;
            }
#endif

            return !String.IsNullOrWhiteSpace(serverName)
                ? $"{serverName.ToLowerInvariant()}:{guid}"
                : guid;
        }

#pragma warning disable 618
        private static IServerProcess WrapProcess(IServerProcess process)
#pragma warning restore 618
        {
            return new InfiniteLoopProcess(new AutomaticRetryProcess(process));
        }

        private static ServerContext GetServerContext(IReadOnlyDictionary<string, object> properties)
        {
            var serverContext = new ServerContext();

            if (properties.ContainsKey("Queues"))
            {
                var array = properties["Queues"] as string[];
                if (array != null)
                {
                    serverContext.Queues = array;
                }
            }

            if (properties.ContainsKey("WorkerCount"))
            {
                serverContext.WorkerCount = (int)properties["WorkerCount"];
            }
            return serverContext;
        }
    }
}
