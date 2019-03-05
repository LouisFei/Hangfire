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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// 循环任务
    /// </summary>
    public static class RecurringJob
    {
        private static readonly Lazy<RecurringJobManager> Instance = new Lazy<RecurringJobManager>(
            () => new RecurringJobManager());

        #region AddOrUpdate
        /// <summary>
        /// 添加或更新任务
        /// </summary>
        /// <param name="methodCall">任务</param>
        /// <param name="cronExpression">执行计划</param>
        /// <param name="timeZone">时区</param>
        /// <param name="queue">消息队列名称（不指定将使用默认名称）</param>
        public static void AddOrUpdate(
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Action> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Action<T>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate(
            Expression<Func<Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Func<T, Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            Expression<Func<Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            var id = GetRecurringJobId(job);

            Instance.Value.AddOrUpdate(id, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }
        
        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Func<Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Func<T, Task>> methodCall,
            Func<string> cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            AddOrUpdate(recurringJobId, methodCall, cronExpression(), timeZone, queue);
        }

        public static void AddOrUpdate(
            string recurringJobId,
            Expression<Func<Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }

        public static void AddOrUpdate<T>(
            string recurringJobId,
            Expression<Func<T, Task>> methodCall,
            string cronExpression,
            TimeZoneInfo timeZone = null,
            string queue = EnqueuedState.DefaultQueue)
        {
            var job = Job.FromExpression(methodCall);
            Instance.Value.AddOrUpdate(recurringJobId, job, cronExpression, timeZone ?? TimeZoneInfo.Utc, queue);
        }
        #endregion

        public static void RemoveIfExists(string recurringJobId)
        {
            Instance.Value.RemoveIfExists(recurringJobId);
        }

        public static void Trigger(string recurringJobId)
        {
            Instance.Value.Trigger(recurringJobId);
        }

        private static string GetRecurringJobId(Job job)
        {
            return $"{job.Type.ToGenericTypeString()}.{job.Method.Name}";
        }
    }
}
