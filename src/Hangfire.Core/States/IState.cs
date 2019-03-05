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

using System.Collections.Generic;
using Hangfire.Annotations;

namespace Hangfire.States
{
    /// <summary>
    /// Provides the essential members for describing a background job state.
    /// 提供描述后台作业状态的基本成员。
    /// </summary>
    /// <remarks>
    /// <para>
    /// Background job processing in Hangfire is all about moving a background job from one state to another. 
    /// Hangfire中的后台作业处理就是将后台作业从一个状态转移到另一个状态。
    /// States are used to clearly decide what to do with a background job. 
    /// States被用来明确决定如何处理后台工作。
    /// For example, <see cref="EnqueuedState"/> tells Hangfire that a job should be processed by a <see cref="Hangfire.Server.Worker"/>, and <see cref="FailedState"/> tells Hangfire that a job should be investigated by a developer.
    /// 例如，EnqueuedState告诉Hangfire一个作业应该由一个工作服务来处理，而FailedState告诉Hangfire一个作业应该由开发人员来调查。
    /// </para> 
    /// <para>
    /// Each state have some essential properties like <see cref="Name"/>, <see cref="IsFinal"/> and a custom ones that are exposed through the <see cref="SerializeData"/> method. 
    /// 每个状态都有一些基本属性，如Name、IsFinal和通过SerializeData方法公开的自定义属性。
    /// Serialized data may be used during the processing stage.
    /// 序列化数据可以在处理阶段使用。
    /// </para>
    /// 
    /// <para>
    /// Hangfire allows you to define custom states to extend the processing pipeline. 
    /// Hangfire允许您定义自定义状态来扩展处理管道。
    /// <see cref="IStateHandler"/> interface implementation can be used to define additional work for a state transition, and <see cref="Server.IBackgroundProcess"/> interface implementation can be used to process background jobs in a new state. 
    /// IStateHandler接口实现可以用于为状态转换定义额外的工作，IBackgroundProcess接口实现可以用于在新状态中处理后台作业。
    /// For example, delayed jobs and their <see cref="ScheduledState"/>, continuations and their <see cref="AwaitingState"/> can be simply moved to an extension package.
    /// 例如，延迟的作业及其ScheduledState、continuation和AwaitingState可以简单地移动到扩展包中。
    /// </para>
    /// </remarks>
    /// 
    /// <example>
    /// <para>
    /// Let's create a new state. 
    /// 让我们创建一个新状态。
    /// Consider you haves background jobs that throw a transient exception from time to time, and you want to simply ignore those exceptions. 
    /// 假设您有一个后台作业，它会不时抛出一个临时异常，您希望简单地忽略这些异常。
    /// By default, Hangfire will move a job that throwed an exception to the <see cref="FailedState"/>, 
    /// 在默认情况下，Hangfire将移动一个将异常抛出到FailedState的作业，
    /// however a job in the <i>failed</i> state will live in a Failed jobs page forever, 
    /// 然而，失败状态下的工作将永远活在失败的工作页面中，
    /// unless we use <see cref="AutomaticRetryAttribute"/>,
    /// 除非我们使用AutomaticRetryAttribute，
    /// delete or retry it manually, because the <see cref="FailedState"/> is not a <i>final</i> state.
    /// 手动删除或重试，因为FailedState不是最终状态。
    /// </para>
    /// 
    /// <para>
    /// Our new state will look like a <see cref="FailedState"/>, but we define the state as a <i>final</i> one, letting Hangfire to expire faulted jobs. 
    /// 我们的新状态看起来像是一个失败的状态，但是我们将该状态定义为最后一个状态，让Hangfire终止有问题的作业。
    /// Please refer to the <see cref="IState"/> interface properties to learn about their details.
    /// 
    /// </para>
    /// 
    /// <para>
    /// In articles related to <see cref="IStateHandler"/> and <see cref="IElectStateFilter"/> interfaces we'll discuss how to use this new state.
    /// 
    /// </para>
    /// 
    /// <code lang="cs" source="..\Samples\States.cs" region="FaultedState" />
    /// </example>
    /// 
    /// <seealso cref="IBackgroundJobStateChanger" />
    /// <seealso cref="IStateHandler" />
    /// <seealso cref="IElectStateFilter" />
    /// <seealso cref="IApplyStateFilter" />
    public interface IState
    {
        /// <summary>
        /// Gets the unique name of the state.
        /// 获取状态的唯一名称。
        /// </summary>
        /// 
        /// <value>
        /// Unique among other states string, that is ready for ordinal comparisons.
        /// 在其他状态中唯一的字符串，可以进行序号比较。
        /// </value>
        /// 
        /// <remarks>
        /// <para>
        /// The state name is used to differentiate one state from another during the state change process. 
        /// 状态名称用于在状态更改过程中区分一个状态和另一个状态。
        /// So all the implemented states should have a <b>unique</b> state name. 
        /// 所以所有实现的状态都应该有唯一的状态名。
        /// Please use one-word names that start with a capital letter, in a past tense in English for your state names, for example:
        /// 请用一个以大写字母开头的单词名，用英语中的过去式来表示你的状态名，例如:
        /// </para>
        /// <list type="bullet">
        ///     <item><c>Succeeded</c></item>
        ///     <item><c>Enqueued</c></item>
        ///     <item><c>Deleted</c></item>
        ///     <item><c>Failed</c></item>
        /// </list>
        /// 
        /// <note type="implement">
        /// The returning value should be hard-coded, no modifications of this property should be allowed to a user. 
        /// 返回值应该是硬编码的，不允许用户修改此属性。
        /// Implementors should not add a public setter on this property.
        /// 实现者不应在此属性上添加公共setter。
        /// </note>
        /// </remarks>
        [NotNull] string Name { get; }

        /// <summary>
        /// Gets the human-readable reason of a state transition.
        /// 获取状态转换的可读原因。
        /// </summary>
        /// 
        /// <value>
        /// Any string with a reasonable length to fit dashboard elements.
        /// 任何长度适合仪表板元素的字符串。
        /// </value>
        /// 
        /// <remarks>
        /// <para>
        /// The reason is usually displayed in the Dashboard UI to simplify the understanding of a background job lifecycle by providing a 
        /// human-readable text that explains why a background job is moved to the corresponding state. 
        /// 原因通常显示在仪表板UI中，通过提供人类可读的文本来解释为什么将后台作业移动到相应的状态，从而简化对后台作业生命周期的理解。
        /// 
        /// Here are some examples:</para>
        /// <list type="bullet">
        ///     <item>
        ///         <i>Can not change the state to 'Enqueued': target method was not found</i>
        ///     </item>
        ///     <item><i>Exceeded the maximum number of retry attempts</i></item>
        /// </list>
        /// <note type="implement">
        /// The reason value is usually not hard-coded in a state implementation,
        /// allowing users to change it when creating an instance of a state through the public setter.
        /// 原因值通常不会在状态实现中硬编码，允许用户在通过公共setter创建状态实例时更改它。
        /// </note>
        /// </remarks>
        [CanBeNull] string Reason { get; }

        /// <summary>
        /// Gets if the current state is a <i>final</i> one.
        /// 获取当前状态是否为最终状态。
        /// </summary>
        /// 
        /// <value><see langword="false" /> for <i>intermediate states</i>,
        /// and <see langword="true" /> for the <i>final</i> ones.</value>
        /// 
        /// <remarks>
        /// <para>
        /// Final states define a termination stage of a background job processing pipeline. 
        /// 最终状态定义后台作业处理管道的终止阶段。
        /// Background jobs in a final state is considered as finished with no further processing required.
        /// 最后状态中的后台作业被认为已经完成，不需要进一步处理。
        /// </para>
        /// 
        /// <para>
        /// The <see cref="IBackgroundJobStateChanger">state machine</see> marks finished background jobs to be expired within an interval that
        /// is defined in the <see cref="ApplyStateContext.JobExpirationTimeout"/>
        /// property that is available from a state changing filter that implements the <see cref="IApplyStateFilter"/> interface.
        /// 
        /// </para>
        /// 
        /// <note type="implement">
        /// When implementing this property, always hard-code this property to
        /// <see langword="true"/> or <see langword="false" />. Hangfire does
        /// not work with states that can be both <i>intermediate</i> and
        /// <i>final</i> yet. Don't define a public setter for this property.
        /// </note>
        /// </remarks>
        /// 
        /// <seealso cref="SucceededState" />
        /// <seealso cref="FailedState" />
        /// <seealso cref="DeletedState" />
        bool IsFinal { get; }

        /// <summary>
        /// Gets whether transition to this state should ignore job de-serialization exceptions.
        /// 获取转换到此状态是否应忽略作业反序列化异常。
        /// </summary>
        /// 
        /// <value><see langword="false"/> to move to the <see cref="FailedState"/> on 
        /// deserialization exceptions, <see langword="true" /> to ignore them.</value>
        /// 
        /// <remarks>
        /// <para>During a state transition, an instance of the <see cref="Common.Job"/> class
        /// is deserialized to get state changing filters, and to allow <see cref="IStateHandler">
        /// state handlers</see> to perform additional work related to the state.</para>
        /// 
        /// <para>However we cannot always deserialize a job, for example, when job method was
        /// removed from the code base or its assembly reference is missing. Since background
        /// processing is impossible anyway, the <see cref="IBackgroundJobStateChanger">state machine</see>
        /// moves such a background job to the <see cref="FailedState"/> in this case to
        /// highlight a problem to the developers (because deserialization exception may
        /// occur due to bad refactorings or other programming mistakes).</para>
        /// 
        /// <para>However, in some exceptional cases we can ignore deserialization exceptions,
        /// and allow a state transition for some states that does not require a <see cref="Common.Job"/>
        /// instance. <see cref="FailedState"/> itself and <see cref="DeletedState"/> are
        /// examples of such a behavior.</para>
        /// 
        /// <note type="implement">
        /// In general, implementers should return <see langword="false"/> when implementing 
        /// this property.
        /// </note>
        /// </remarks>
        /// 
        /// <seealso cref="FailedState"/>
        /// <seealso cref="DeletedState"/>
        bool IgnoreJobLoadException { get; }

        /// <summary>
        /// Gets a serialized representation of the current state. 
        /// 获取当前状态的序列化表示。
        /// </summary>
        /// <remarks>
        /// Returning dictionary contains the serialized properties of a state. You can obtain 
        /// the state data by using the <see cref="Storage.IStorageConnection.GetStateData"/>
        /// method. Please refer to documentation for this method in implementors to learn
        /// which key/value pairs are available.
        /// </remarks>
        /// <returns>A dictionary with serialized properties of the current state.</returns>
        [NotNull] Dictionary<string, string> SerializeData();
    }
}
