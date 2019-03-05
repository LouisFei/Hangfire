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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using NCrontab;

namespace Hangfire
{
    /// <summary>
    /// 循环任务管理器
    /// Represents a recurring job manager that allows to create, update or delete recurring jobs.
    /// 表示允许创建、更新或删除重复作业的重复作业管理器。
    /// </summary>
    public class RecurringJobManager : IRecurringJobManager
    {
        private readonly JobStorage _storage;
        private readonly IBackgroundJobFactory _factory;

        public RecurringJobManager()
            : this(JobStorage.Current)
        {
        }

        public RecurringJobManager([NotNull] JobStorage storage)
            : this(storage, new BackgroundJobFactory())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storage">任务仓库</param>
        /// <param name="factory">后台任务工厂</param>
        public RecurringJobManager([NotNull] JobStorage storage, [NotNull] IBackgroundJobFactory factory)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _storage = storage;
            _factory = factory;
        }

        /// <summary>
        /// 添加或修改作业
        /// </summary>
        /// <param name="recurringJobId">作业标识符</param>
        /// <param name="job">作业</param>
        /// <param name="cronExpression">执行计划</param>
        /// <param name="options">循环作业选项</param>
        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (cronExpression == null) throw new ArgumentNullException(nameof(cronExpression));
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            //验证执行计划Cron表达式
            ValidateCronExpression(cronExpression);

            using (var connection = _storage.GetConnection())
            {
                var recurringJob = new Dictionary<string, string>();
                var invocationData = InvocationData.Serialize(job);

                recurringJob["Job"] = JobHelper.ToJson(invocationData);
                recurringJob["Cron"] = cronExpression;
                recurringJob["TimeZoneId"] = options.TimeZone.Id;
                recurringJob["Queue"] = options.QueueName;

                var existingJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
                if (existingJob == null)
                {
                    recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(DateTime.UtcNow);
                }

                using (var transaction = connection.CreateWriteTransaction())
                {
                    transaction.SetRangeInHash(
                        $"recurring-job:{recurringJobId}",
                        recurringJob);

                    transaction.AddToSet("recurring-jobs", recurringJobId);

                    transaction.Commit();
                }
            }
        }

        public void Trigger(string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            using (var connection = _storage.GetConnection())
            {
                var hash = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
                if (hash == null)
                {
                    return;
                }
                
                var job = JobHelper.FromJson<InvocationData>(hash["Job"]).Deserialize();
                var state = new EnqueuedState { Reason = "Triggered using recurring job manager" };

                if (hash.ContainsKey("Queue"))
                {
                    state.Queue = hash["Queue"];
                }

                var context = new CreateContext(_storage, connection, job, state);
                context.Parameters["RecurringJobId"] = recurringJobId;
                _factory.Create(context);
            }
        }

        public void RemoveIfExists(string recurringJobId)
        {
            if (recurringJobId == null) throw new ArgumentNullException(nameof(recurringJobId));

            using (var connection = _storage.GetConnection())
            using (var transaction = connection.CreateWriteTransaction())
            {
                transaction.RemoveHash($"recurring-job:{recurringJobId}");
                transaction.RemoveFromSet("recurring-jobs", recurringJobId);

                transaction.Commit();
            }
        }

        /// <summary>
        /// 验证Cron表达式是否合法
        /// </summary>
        /// <remarks>
        /// https://github.com/atifaziz/NCrontab
        /// </remarks>
        /// <param name="cronExpression"></param>
        private static void ValidateCronExpression(string cronExpression)
        {
            try
            {
                var schedule = CrontabSchedule.Parse(cronExpression);
                schedule.GetNextOccurrence(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("CRON expression is invalid. Please see the inner exception for details.", nameof(cronExpression), ex);
            }
        }
    }
}
