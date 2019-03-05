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

namespace Hangfire.Server
{
    /// <summary>
    /// 后台进程上下文环境
    /// </summary>
    public class BackgroundProcessContext
    {
        /// <summary>
        /// 实例化后台进程上下文环境对象
        /// </summary>
        /// <param name="serverId">服务器编号</param>
        /// <param name="storage">存储方式</param>
        /// <param name="properties">属性集合</param>
        /// <param name="cancellationToken">取消操作标记</param>
        public BackgroundProcessContext(
            [NotNull] string serverId,
            [NotNull] JobStorage storage, 
            [NotNull] IDictionary<string, object> properties, 
            CancellationToken cancellationToken)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            ServerId = serverId;
            Storage = storage;
            Properties = new Dictionary<string, object>(properties, StringComparer.OrdinalIgnoreCase);
            CancellationToken = cancellationToken;
        }
        
        [NotNull]
        public string ServerId { get; }

        [NotNull]
        public IReadOnlyDictionary<string, object> Properties { get; }

        [NotNull]
        public JobStorage Storage { get; }

        public CancellationToken CancellationToken { get; }

        public bool IsShutdownRequested => CancellationToken.IsCancellationRequested; //获取是否已请求取消此标记

        public void Wait(TimeSpan timeout)
        {
            CancellationToken.Wait(timeout);
        }
    }
}