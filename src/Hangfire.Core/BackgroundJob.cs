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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Provides static methods for creating <i>fire-and-forget</i>, <i>delayed</i> jobs and <i>continuations</i> as well as re-queue and delete existing background jobs.
    /// 提供静态方法来创建<i>即发即忘</i>， <i>延迟</i>作业和<i>延续</i>以及重新排队和删除现有后台作业。
    /// </summary>
    /// <remarks>
    /// <para>This class is a wrapper for the <see cref="IBackgroundJobClient"/> interface 
    /// 这个类是一个对IBackgroundJobClient接口的包装，
    /// and its default implementation, <see cref="BackgroundJobClient"/> class, 
    /// 它的默认实现是BackgroundJobClient类，
    /// that was created for the most simple scenarios. 
    /// 这是为最简单的场景创建的。
    /// Please consider using the types above in real world applications.
    /// 请考虑在实际应用程序中使用上述类型。</para>
    /// <para>This class also contains undocumented constructor and instance members. 
    /// 该类还包含无文档记录的构造函数和实例成员。
    /// They are hidden to not to confuse new users. You can freely use them in low-level API.
    /// 隐藏它们是为了不让新用户感到困惑。您可以在低级API中自由地使用它们。
    /// </para>
    /// </remarks>
    /// 
    /// <seealso cref="IBackgroundJobClient"/>
    /// <seealso cref="BackgroundJobClient"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public partial class BackgroundJob
    {
        private static readonly Lazy<IBackgroundJobClient> CachedClient 
            = new Lazy<IBackgroundJobClient>(() => new BackgroundJobClient()); 

        private static readonly Func<IBackgroundJobClient> DefaultFactory
            = () => CachedClient.Value;

        private static Func<IBackgroundJobClient> _clientFactory;
        private static readonly object ClientFactoryLock = new object();

        internal static Func<IBackgroundJobClient> ClientFactory
        {
            get
            {
                lock (ClientFactoryLock)
                {
                    return _clientFactory ?? DefaultFactory;
                }
            }
            set
            {
                lock (ClientFactoryLock)
                {
                    _clientFactory = value;
                }
            }
        }

        #region Enqueue
        /// <summary>
        /// Creates a new fire-and-forget job based on a given method call expression.
        /// 基于给定的方法调用表达式创建一个新的“即发即忘”作业。
        /// </summary>
        /// <param name="methodCall">
        /// Method call expression that will be marshalled to a server.
        /// 将编组到服务器的方法调用表达式。
        /// </param>
        /// <returns>
        /// Unique identifier of a background job.
        /// 后台作业的唯一标识符。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="methodCall"/> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="EnqueuedState"/>
        /// <seealso cref="O:Hangfire.IBackgroundJobClient.Enqueue"/>
        public static string Enqueue([NotNull, InstantHandle] Expression<Action> methodCall)
        {
            var client = ClientFactory();
            return client.Enqueue(methodCall);
        }

        /// <summary>
        /// Creates a new fire-and-forget job based on a given method call expression.
        /// 基于给定的方法调用表达式创建一个新的“即发即忘”作业。
        /// </summary>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a background job.</returns>
        /// 
        /// <exception cref="ArgumentNullException">
        /// <paramref name="methodCall"/> is <see langword="null"/>.
        /// </exception>
        /// 
        /// <seealso cref="EnqueuedState"/>
        /// <seealso cref="O:Hangfire.IBackgroundJobClient.Enqueue"/>
        public static string Enqueue([NotNull, InstantHandle] Expression<Func<Task>> methodCall)
        {
            var client = ClientFactory();
            return client.Enqueue(methodCall);
        }

        /// <summary>
        /// Creates a new fire-and-forget job based on a given method call expression.
        /// 基于给定的方法调用表达式创建一个新的“即发即忘”作业。
        /// </summary>
        /// <typeparam name="T">方法调用中的参数类型</typeparam>
        /// <param name="methodCall">
        /// Method call expression that will be marshalled to a server.
        /// 将编组到服务器的方法调用表达式。
        /// </param>
        /// <returns>Unique identifier of a background job.</returns>
        /// 
        /// <exception cref="ArgumentNullException">
        /// <paramref name="methodCall"/> is <see langword="null"/>.
        /// </exception>
        /// 
        /// <seealso cref="EnqueuedState"/>
        /// <seealso cref="O:Hangfire.IBackgroundJobClient.Enqueue"/>
        public static string Enqueue<T>([NotNull, InstantHandle] Expression<Action<T>> methodCall)
        {
            var client = ClientFactory();
            return client.Enqueue(methodCall);
        }

        /// <summary>
        /// Creates a new fire-and-forget job based on a given method call expression.
        /// 基于给定的方法调用表达式创建一个新的“即发即忘”作业。
        /// </summary>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a background job.</returns>
        /// 
        /// <exception cref="ArgumentNullException">
        /// <paramref name="methodCall"/> is <see langword="null"/>.
        /// </exception>
        /// 
        /// <seealso cref="EnqueuedState"/>
        /// <seealso cref="O:Hangfire.IBackgroundJobClient.Enqueue"/>
        public static string Enqueue<T>([NotNull, InstantHandle] Expression<Func<T, Task>> methodCall)
        {
            var client = ClientFactory();
            return client.Enqueue(methodCall);
        }
        #endregion

        #region Schedule
        /// <summary>
        /// Creates a new background job based on a specified method call expression and schedules it to be enqueued after a given delay.
        /// 基于指定的方法调用表达式创建一个新的后台作业，并在给定的延迟后将其排入队列。
        /// </summary>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule(
            [NotNull, InstantHandle] Expression<Action> methodCall, 
            TimeSpan delay)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, delay);
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression and schedules it to be enqueued after a given delay.
        /// 基于指定的方法调用表达式创建一个新的后台作业，并在给定的延迟后将其排入队列。
        /// </summary>
        /// <param name="methodCall">
        /// Instance method call expression that will be marshalled to the Server.
        /// 将编组到服务器的实例方法调用表达式。
        /// </param>
        /// <param name="delay">
        /// Delay, after which the job will be enqueued.
        /// 延迟，之后作业将被加入队列。
        /// </param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule(
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            TimeSpan delay)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, delay);
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression and schedules it to be enqueued at the given moment of time.
        /// 根据指定的方法调用表达式创建一个新的后台作业，并将其安排在给定时刻排队。
        /// </summary>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">
        /// The moment of time at which the job will be enqueued.
        /// 作业排队的时刻。
        /// </param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule(
            [NotNull, InstantHandle] Expression<Action> methodCall, 
            DateTimeOffset enqueueAt)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, enqueueAt);
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression and schedules it to be enqueued at the given moment of time.
        /// </summary>
        /// 
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule(
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            DateTimeOffset enqueueAt)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, enqueueAt);
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method call expression and schedules it to be enqueued after a given delay.
        /// 基于指定的实例方法调用表达式创建一个新的后台作业，并在给定的延迟后将其排入队列。
        /// </summary>
        /// <typeparam name="T">
        /// Type whose method will be invoked during job processing.
        /// 类型，其方法将在作业处理期间调用。
        /// </typeparam>
        /// <param name="methodCall">
        /// Instance method call expression that will be marshalled to the Server.
        /// 将编组到服务器的实例方法调用表达式。
        /// </param>
        /// <param name="delay">
        /// Delay, after which the job will be enqueued.
        /// 延迟，在此之后作业将进入队列。
        /// </param>
        /// <returns>
        /// Unique identifier of the created job.
        /// 所创建作业的唯一标识符。
        /// </returns>
        public static string Schedule<T>(
            [NotNull, InstantHandle] Expression<Action<T>> methodCall, 
            TimeSpan delay)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, delay);
        }

        /// <summary>
        /// Creates a new background job based on a specified instance method
        /// call expression and schedules it to be enqueued after a given delay.
        /// </summary>
        /// 
        /// <typeparam name="T">Type whose method will be invoked during job processing.</typeparam>
        /// <param name="methodCall">Instance method call expression that will be marshalled to the Server.</param>
        /// <param name="delay">Delay, after which the job will be enqueued.</param>
        /// <returns>Unique identifier of the created job.</returns>
        public static string Schedule<T>(
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            TimeSpan delay)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, delay);
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression
        /// and schedules it to be enqueued at the given moment of time.
        /// </summary>
        /// 
        /// <typeparam name="T">The type whose method will be invoked during the job processing.</typeparam>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule<T>(
            [NotNull, InstantHandle] Expression<Action<T>> methodCall, 
            DateTimeOffset enqueueAt)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, enqueueAt);
        }

        /// <summary>
        /// Creates a new background job based on a specified method call expression
        /// and schedules it to be enqueued at the given moment of time.
        /// </summary>
        /// 
        /// <typeparam name="T">The type whose method will be invoked during the job processing.</typeparam>
        /// <param name="methodCall">Method call expression that will be marshalled to the Server.</param>
        /// <param name="enqueueAt">The moment of time at which the job will be enqueued.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string Schedule<T>(
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            DateTimeOffset enqueueAt)
        {
            var client = ClientFactory();
            return client.Schedule(methodCall, enqueueAt);
        }
        #endregion

        #region Delete
        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/> to the <see cref="DeletedState"/>. 
        /// 将指定的<paramref name="jobId"/>作业状态更改为<see cref="DeletedState"/>。
        /// <seealso cref="BackgroundJobClientExtensions.Delete(IBackgroundJobClient, string)"/>
        /// </summary>
        /// 
        /// <param name="jobId">
        /// An identifier, that will be used to find a job.
        /// 用于查找作业的标识符。
        /// </param>
        /// <returns>True on a successfull state transition, false otherwise.</returns>
        public static bool Delete([NotNull] string jobId)
        {
            var client = ClientFactory();
            return client.Delete(jobId);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/> to the <see cref="DeletedState"/>. 
        /// 将指定的<paramref name="jobId"/>作业状态更改为<see cref="DeletedState"/>。
        /// State change is only performed if current job state is equal to the <paramref name="fromState"/> value.
        /// 只有当当前作业状态等于<paramref name="fromState"/>值时，才会执行状态更改。
        /// <seealso cref="BackgroundJobClientExtensions.Delete(IBackgroundJobClient, string, string)"/>
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Delete([NotNull] string jobId, [CanBeNull] string fromState)
        {
            var client = ClientFactory();
            return client.Delete(jobId, fromState);
        }
        #endregion

        #region Requeue
        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/> to the <see cref="EnqueuedState"/>.
        /// 将指定的<paramref name="jobId"/>作业状态更改为<see cref="EnqueuedState"/>。
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Requeue([NotNull] string jobId)
        {
            var client = ClientFactory();
            return client.Requeue(jobId);
        }

        /// <summary>
        /// Changes state of a job with the specified <paramref name="jobId"/>
        /// to the <see cref="EnqueuedState"/>. If <paramref name="fromState"/> value 
        /// is not null, state change will be performed only if the current state name 
        /// of a job equal to the given value.
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of job, whose state is being changed.</param>
        /// <param name="fromState">Current state assertion, or null if unneeded.</param>
        /// <returns>True, if state change succeeded, otherwise false.</returns>
        public static bool Requeue([NotNull] string jobId, [CanBeNull] string fromState)
        {
            var client = ClientFactory();
            return client.Requeue(jobId, fromState);
        }
        #endregion

        #region ContinueWith
        /// <summary>
        /// Creates a new background job that will wait for a successful completion of another background job to be enqueued.
        /// 创建一个新的后台作业，该作业将等待另一个后台作业成功完成后加入队列。
        /// </summary>
        /// <param name="parentId">
        /// Identifier of a background job to wait completion for.
        /// 要等待完成的后台作业的标识符。
        /// </param>
        /// <param name="methodCall">
        /// Method call expression that will be marshalled to a server.
        /// 将编组到服务器的方法调用表达式。
        /// </param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueWith(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action> methodCall)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall);
        }

        /// <summary>
        /// Creates a new background job that will wait for a successful completion of another background job to be enqueued.
        /// 创建一个新的后台作业，该作业将等待另一个后台作业的成功完成来排队。
        /// </summary>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueWith<T>(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action<T>> methodCall)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be enqueued.
        /// </summary>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueWith(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action> methodCall, 
            JobContinuationOptions options)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be enqueued.
        /// </summary>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="options">Continuation options. By default, 
        /// <see cref="JobContinuationOptions.OnlyOnSucceededState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueWith(
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Func<Task>> methodCall,
            JobContinuationOptions options = JobContinuationOptions.OnlyOnSucceededState)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options: options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be enqueued.
        /// </summary>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="options">Continuation options.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueWith<T>(
            [NotNull] string parentId, 
            [NotNull, InstantHandle] Expression<Action<T>> methodCall, 
            JobContinuationOptions options)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options);
        }

        /// <summary>
        /// Creates a new background job that will wait for another background job to be enqueued.
        /// </summary>
        /// <param name="parentId">Identifier of a background job to wait completion for.</param>
        /// <param name="methodCall">Method call expression that will be marshalled to a server.</param>
        /// <param name="options">Continuation options. By default, 
        /// <see cref="JobContinuationOptions.OnlyOnSucceededState"/> is used.</param>
        /// <returns>Unique identifier of a created job.</returns>
        public static string ContinueWith<T>(
            [NotNull] string parentId,
            [NotNull, InstantHandle] Expression<Func<T, Task>> methodCall,
            JobContinuationOptions options = JobContinuationOptions.OnlyOnSucceededState)
        {
            var client = ClientFactory();
            return client.ContinueWith(parentId, methodCall, options: options);
        }
        #endregion
    }
}
