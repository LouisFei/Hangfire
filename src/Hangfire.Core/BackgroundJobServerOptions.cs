// This file is part of Hangfire.
// Copyright ?2013-2014 Sergey Odinokov.
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
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// 后台作业服务设置
    /// </summary>
    public class BackgroundJobServerOptions
    {
        // https://github.com/HangfireIO/Hangfire/issues/246
        /// <summary>
        /// 默认的最大工作者数量
        /// </summary>
        private const int MaxDefaultWorkerCount = 20;

        private int _workerCount;
        private string[] _queues;

        public BackgroundJobServerOptions()
        {
            WorkerCount = Math.Min(Environment.ProcessorCount * 5, MaxDefaultWorkerCount);
            Queues = new[] { EnqueuedState.DefaultQueue };
            ShutdownTimeout = BackgroundProcessingServer.DefaultShutdownTimeout;
            SchedulePollingInterval = DelayedJobScheduler.DefaultPollingDelay;
            HeartbeatInterval = ServerHeartbeat.DefaultHeartbeatInterval;
            ServerTimeout = ServerWatchdog.DefaultServerTimeout;
            ServerCheckInterval = ServerWatchdog.DefaultCheckInterval;
            
            FilterProvider = null;
            Activator = null;
        }
        
        public string ServerName { get; set; }

        public int WorkerCount
        {
            get { return _workerCount; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "WorkerCount property value should be positive.");

                _workerCount = value;
            }
        }

        /// <summary>
        /// 队列名称
        /// </summary>
        public string[] Queues
        {
            get { return _queues; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length == 0) throw new ArgumentException("You should specify at least one queue to listen.", nameof(value));

                _queues = value;
            }
        }

        /// <summary>
        /// 关闭超时
        /// </summary>
        public TimeSpan ShutdownTimeout { get; set; }
        /// <summary>
        /// 计划轮询间隔
        /// </summary>
        public TimeSpan SchedulePollingInterval { get; set; }
        /// <summary>
        /// 心跳间隔
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; }
        /// <summary>
        /// 服务器超时
        /// </summary>
        public TimeSpan ServerTimeout { get; set; }
        /// <summary>
        /// 服务器检查间隔
        /// </summary>
        public TimeSpan ServerCheckInterval { get; set; }

        [Obsolete("Please use `ServerTimeout` or `ServerCheckInterval` options instead. Will be removed in 2.0.0.")]
        public ServerWatchdogOptions ServerWatchdogOptions { get; set; }

        [CanBeNull]
        public IJobFilterProvider FilterProvider { get; set; }

        [CanBeNull]
        public JobActivator Activator { get; set; }
    }
}