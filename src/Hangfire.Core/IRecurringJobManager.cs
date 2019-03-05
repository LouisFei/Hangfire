// This file is part of Hangfire.
// Copyright © 2016 Sergey Odinokov.
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

using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire
{
    /// <summary>
    /// 循环任务管理器接口
    /// </summary>
    public interface IRecurringJobManager
    {
        /// <summary>
        /// 添加或修改任务
        /// </summary>
        /// <param name="recurringJobId"></param>
        /// <param name="job"></param>
        /// <param name="cronExpression"></param>
        /// <param name="options"></param>
        void AddOrUpdate(
            [NotNull] string recurringJobId, 
            [NotNull] Job job, 
            [NotNull] string cronExpression, 
            [NotNull] RecurringJobOptions options);

        /// <summary>
        /// 触发任务
        /// </summary>
        /// <param name="recurringJobId"></param>
        void Trigger([NotNull] string recurringJobId);

        /// <summary>
        /// 删除任务（如果任务存在的话）
        /// </summary>
        /// <param name="recurringJobId"></param>
        void RemoveIfExists([NotNull] string recurringJobId);
    }
}