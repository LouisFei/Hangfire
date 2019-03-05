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
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.States;

namespace Hangfire.Storage
{
    /// <summary>
    /// 只写事务接口
    /// </summary>
    public interface IWriteOnlyTransaction : IDisposable
    {
        // Job operations 作业操作
        void ExpireJob([NotNull] string jobId, TimeSpan expireIn);
        void PersistJob([NotNull] string jobId);
        void SetJobState([NotNull] string jobId, [NotNull] IState state);
        void AddJobState([NotNull] string jobId, [NotNull] IState state);

        // Queue operations 队列操作
        void AddToQueue([NotNull] string queue, [NotNull] string jobId);

        // Counter operations  计数器操作
        void IncrementCounter([NotNull] string key);
        void IncrementCounter([NotNull] string key, TimeSpan expireIn);
        void DecrementCounter([NotNull] string key);
        void DecrementCounter([NotNull] string key, TimeSpan expireIn);

        // Set operations 集合操作
        void AddToSet([NotNull] string key, [NotNull] string value);
        void AddToSet([NotNull] string key, [NotNull] string value, double score);
        void RemoveFromSet([NotNull] string key, [NotNull] string value);

        // List operations 列表操作
        void InsertToList([NotNull] string key, [NotNull] string value);
        void RemoveFromList([NotNull] string key, [NotNull] string value);
        void TrimList([NotNull] string key, int keepStartingFrom, int keepEndingAt);

        // Hash operations Hash操作
        void SetRangeInHash([NotNull] string key, [NotNull] IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        void RemoveHash([NotNull] string key);

        void Commit();
    }
}