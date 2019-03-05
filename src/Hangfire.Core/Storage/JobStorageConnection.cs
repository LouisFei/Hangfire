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
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire.Storage
{
    /// <summary>
    /// 作业仓库连接抽象基类
    /// </summary>
    public abstract class JobStorageConnection : IStorageConnection
    {
        public virtual void Dispose()
        {
        }

        // Common 通用
        /// <summary>
        /// 创建只写事务
        /// </summary>
        /// <returns></returns>
        public abstract IWriteOnlyTransaction CreateWriteTransaction();
        /// <summary>
        /// 获得分布式锁
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public abstract IDisposable AcquireDistributedLock(string resource, TimeSpan timeout);

        // Background jobs 后台作业
        /// <summary>
        /// 创建会过期作业
        /// </summary>
        /// <param name="job">作业</param>
        /// <param name="parameters">参数</param>
        /// <param name="createdAt">创建时间</param>
        /// <param name="expireIn">到期时间</param>
        /// <returns></returns>
        public abstract string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt, TimeSpan expireIn);

        /// <summary>
        /// 获取下一个作业
        /// </summary>
        /// <param name="queues">队列</param>
        /// <param name="cancellationToken">可取消操作的标记</param>
        /// <returns></returns>
        public abstract IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken);

        /// <summary>
        /// 设置作业参数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public abstract void SetJobParameter(string id, string name, string value);

        /// <summary>
        /// 获得作业参数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract string GetJobParameter(string id, string name);

        /// <summary>
        /// 获得作业数据
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public abstract JobData GetJobData(string jobId);

        /// <summary>
        /// 获得作业状态数据
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public abstract StateData GetStateData(string jobId);

        // Servers 服务器
        /// <summary>
        /// 宣布服务器
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="context"></param>
        public abstract void AnnounceServer(string serverId, ServerContext context);
        public abstract void RemoveServer(string serverId);
        public abstract void Heartbeat(string serverId);
        public abstract int RemoveTimedOutServers(TimeSpan timeOut);

        // Sets 集合
        public abstract HashSet<string> GetAllItemsFromSet(string key);
        public abstract string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);

        public virtual long GetSetCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual List<string> GetRangeFromSet([NotNull] string key, int startingFrom, int endingAt)
        {
            throw new NotSupportedException();
        }

        public virtual TimeSpan GetSetTtl([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        // Hashes 哈希
        public abstract void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        public abstract Dictionary<string, string> GetAllEntriesFromHash(string key);

        public virtual string GetValueFromHash([NotNull] string key, [NotNull] string name)
        {
            throw new NotSupportedException();
        }

        public virtual long GetHashCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual TimeSpan GetHashTtl([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        // Lists 列表
        public virtual long GetListCount([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual List<string> GetAllItemsFromList([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        public virtual List<string> GetRangeFromList([NotNull] string key, int startingFrom, int endingAt)
        {
            throw new NotSupportedException();
        }

        public virtual TimeSpan GetListTtl([NotNull] string key)
        {
            throw new NotSupportedException();
        }

        // Counters 计数器
        public virtual long GetCounter([NotNull] string key)
        {
            throw new NotSupportedException();
        }
    }
}