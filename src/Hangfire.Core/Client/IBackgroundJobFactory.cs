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

using Hangfire.Annotations;

namespace Hangfire.Client
{
    /// <summary>
    /// This interface acts as extensibility point for the process of job creation. 
    /// 该接口充当创建作业过程的可扩展性点。
    /// See the default implementation in the <see cref="BackgroundJobFactory"/> class.
    /// </summary>
    public interface IBackgroundJobFactory
    {
        /// <summary>
        /// Runs the process of job creation with the specified context.
        /// 在指定的上下文中运行作业创建过程。
        /// </summary>
        [CanBeNull]
        BackgroundJob Create([NotNull] CreateContext context);
    }
}