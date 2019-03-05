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

namespace Hangfire.States
{
    /// <summary>
    /// 后台作业状态改变器
    /// </summary>
    public interface IBackgroundJobStateChanger
    {
        /// <summary>
        /// Attempts to change the state of a job, respecting any applicable job filters and state handlers.
        /// 尝试更改作业的状态，并尊重任何适用的作业筛选器和状态处理程序。
        /// </summary>
        /// <returns><c>Null</c> if a constraint has failed, otherwise the final applied state</returns>
        /// <remarks>
        /// Also ensures that the job data can be loaded for this job
        /// 还要确保可以为此作业加载作业数据
        /// </remarks>
        IState ChangeState(StateChangeContext context);
    }
}