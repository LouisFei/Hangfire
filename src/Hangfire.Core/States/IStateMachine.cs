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

namespace Hangfire.States
{
    /// <summary>
    /// Provides a mechanism for running state election and state applying processes.
    /// 提供一种运行状态选择和应用流程的机制。
    /// </summary>
    /// 
    /// <seealso cref="StateMachine"/>
    public interface IStateMachine
    {
        /// <summary>
        /// Performs the state applying process, where a current background job will be moved to the elected state.
        /// 执行状态申请流程，其中当前后台作业将被移动到所选的状态。
        /// </summary>
        /// <param name="context">
        /// The context of a state applying process.
        /// 状态应用流程的上下文
        /// </param>
        IState ApplyState(ApplyStateContext context);
    }
}
