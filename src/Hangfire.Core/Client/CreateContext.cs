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
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Client
{
    /// <summary>
    /// Provides information about the context in which the job is created.
    /// 提供有关创建作业的上下文的信息。
    /// </summary>
    public class CreateContext
    {
        public CreateContext([NotNull] CreateContext context)
            : this(context.Storage, context.Connection, context.Job, context.InitialState)
        {
            Items = context.Items;
            Parameters = context.Parameters;
        }

        /// <summary>
        /// 创建作业上下文
        /// </summary>
        /// <param name="storage">作业存储</param>
        /// <param name="connection">作业仓库连接</param>
        /// <param name="job">作业实例</param>
        /// <param name="initialState">作业初始状态</param>
        public CreateContext(
            [NotNull] JobStorage storage, 
            [NotNull] IStorageConnection connection, 
            [NotNull] Job job, 
            [CanBeNull] IState initialState)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (job == null) throw new ArgumentNullException(nameof(job));

            Storage = storage;
            Connection = connection;
            Job = job;
            InitialState = initialState;

            Items = new Dictionary<string, object>();
            Parameters = new Dictionary<string, object>();
        }

        [NotNull]
        public JobStorage Storage { get; }

        [NotNull]
        public IStorageConnection Connection { get; }

        /// <summary>
        /// Gets an instance of the key-value storage. 
        /// 获取键值存储的实例。
        /// You can use it to pass additional information between different client filters or just between different methods.
        /// 您可以使用它在不同的客户端过滤器之间传递额外的信息，或者只是在不同的方法之间传递信息。
        /// </summary>
        [NotNull]
        public IDictionary<string, object> Items { get; }

        [NotNull]
        public virtual IDictionary<string, object> Parameters { get; }
            
        [NotNull]
        public Job Job { get; }

        /// <summary>
        /// Gets the initial state of the creating job. 
        /// 获取创建作业的初始状态。
        /// Note, that the final state of the created job could be changed after the registered instances of the <see cref="IElectStateFilter"/> class are doing their job.
        /// 注意，创建的作业的最终状态可以在<see cref="IElectStateFilter"/>类的注册实例完成它们的工作之后更改。
        /// </summary>
        [CanBeNull]
        public IState InitialState { get; }
    }
}