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
using System.Globalization;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>final</i> state of a background job when a <see cref="Server.Worker"/> performed an <i>enqueued</i> job without any exception thrown during the performance.
    /// 定义后台作业的最终状态，该后台作业是指工作人员在执行排队作业时没有在执行过程中引发任何异常。
    /// </summary>
    /// <remarks>
    /// <para>
    /// All the transitions to the <i>Succeeded</i> state are internal for the <see cref="Server.Worker"/> background process. 
    /// 所有到成功状态的转换都是Worker后台进程的内部转换。
    /// You can't create background jobs using this state, and can't change state to <i>Succeeded</i>.
    /// 您不能使用此状态创建后台作业，也不能将状态更改为成功。
    /// </para>
    /// <para>
    /// This state is used in a user code primarily in state change filters (TODO: add a link) to add custom logic during state transitions.
    /// 此状态主要用于状态更改筛选器(TODO: add a link)中的用户代码中，用于在状态转换期间添加自定义逻辑。
    /// </para> 
    /// </remarks> 
    /// 
    /// <seealso cref="EnqueuedState"/>
    /// <seealso cref="Server.Worker"/>
    /// <seealso cref="IState"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public class SucceededState : IState
    {
        /// <summary>
        /// Represents the name of the <i>Succeeded</i> state. This field is read-only.
        /// 表示成功状态的名称。该字段是只读的。
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Succeeded"</c>.
        /// </remarks>
        public static readonly string StateName = "Succeeded";

        internal SucceededState(object result, long latency, long performanceDuration)
        {
            SucceededAt = DateTime.UtcNow;
            Result = result;
            Latency = latency;
            PerformanceDuration = performanceDuration;
        }

        /// <summary>
        /// Gets a date/time when the current state instance was created.
        /// 获取创建当前状态实例的日期/时间。
        /// </summary>
        public DateTime SucceededAt { get; }

        /// <summary>
        /// Gets the value returned by a job method.
        /// 获取作业方法返回的值。
        /// </summary>
        public object Result { get; }

        /// <summary>
        /// Gets the total number of milliseconds passed from a job creation time till the start of the performance.
        /// 获取从作业创建时间到性能开始的总毫秒数。
        /// </summary>
        public long Latency { get; }

        /// <summary>
        /// Gets the total milliseconds elapsed from a processing start.
        /// 获取处理启动所经过的总毫秒数。
        /// </summary>
        public long PerformanceDuration { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="SucceededState"/>.
        /// 总是等于成功状态的StateName。
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see> article for the details.
        /// Please see the remarks section of the IStateName article for the details.
        /// </remarks>
        public string Name => StateName;

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="true"/> for the <see cref="SucceededState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation for the details.
        /// </remarks>
        public bool IsFinal => true;

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="SucceededState"/>.
        /// Please see the description of this property in the <see cref="IState.IgnoreJobLoadException">IState.IgnoreJobLoadException</see> article.
        /// 请参阅IgnoreJobLoadException文章中对该属性的描述。
        /// </remarks>
        public bool IgnoreJobLoadException => false;

        /// <summary>
        /// 序列化数据
        /// </summary>
        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// Returning dictionary contains the following keys. 
        /// 返回字典包含以下键。
        /// You can obtain the state data by using the <see cref="IStorageConnection.GetStateData"/> method.
        /// </para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Key</term>
        ///         <term>Type</term>
        ///         <term>Deserialize Method</term>
        ///         <description>Notes</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>SucceededAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="SucceededAt"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>PerformanceDuration</c></term>
        ///         <term><see cref="long"/></term>
        ///         <term>
        ///             <see cref="Int64.Parse(string, IFormatProvider)"/> with 
        ///             <see cref="CultureInfo.InvariantCulture"/>
        ///         </term>
        ///         <description>Please see the <see cref="PerformanceDuration"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>Latency</c></term>
        ///         <term><see cref="long"/></term>
        ///         <term>
        ///             <see cref="Int64.Parse(string, IFormatProvider)"/> with 
        ///             <see cref="CultureInfo.InvariantCulture"/>
        ///         </term>
        ///         <description>Please see the <see cref="Latency"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>Result</c></term>
        ///         <term><see cref="object"/></term>
        ///         <term><see cref="JobHelper.FromJson"/></term>
        ///         <description>
        ///             <para>Please see the <see cref="Result"/> property.</para>
        ///             <para>This key may be missing from the dictionary, when the return 
        ///             value was <see langword="null" />. Always check for its existence 
        ///             before using it.</para>
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            var data = new Dictionary<string, string>
            {
                { "SucceededAt",  JobHelper.SerializeDateTime(SucceededAt) },
                { "PerformanceDuration", PerformanceDuration.ToString(CultureInfo.InvariantCulture) },
                { "Latency", Latency.ToString(CultureInfo.InvariantCulture) }
            };

            if (Result != null)
            {
                string serializedResult;

                try
                {
                    serializedResult = JobHelper.ToJson(Result);
                }
                catch (Exception)
                {
                    serializedResult = "Can not serialize the return value";
                }

                if (serializedResult != null)
                {
                    data.Add("Result", serializedResult);
                }
            }

            return data;
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.IncrementCounter("stats:succeeded");
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.DecrementCounter("stats:succeeded");
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string StateName => SucceededState.StateName;
        }
    }
}
