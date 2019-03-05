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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Provides methods for creating all the types of background jobs and changing their states. 
    /// 提供创建所有后台作业类型并更改其状态的方法。
    /// Represents a default implementation of the <see cref="IBackgroundJobClient"/> interface.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    /// This class uses the <see cref="IBackgroundJobFactory"/> interface for creating background jobs and the <see cref="IBackgroundJobStateChanger"/> interface for changing their states. 
    /// 该类使用IBackgroundJobFactory接口创建后台作业，使用IBackgroundJobStateChanger接口更改它们的状态。
    /// Please see documentation for those types and their implementations to learn the details.
    /// 有关这些类型及其实现的详细信息，请参阅文档。
    /// </para>
    /// 
    /// <note type="warning">
    /// Despite the fact that instance methods of this class are thread-safe, most implementations of the <see cref="IState"/> interface are <b>neither thread-safe, nor immutable</b>. 
    /// 尽管此类的实例方法是线程安全的，但IState接口的大多数实现既不是线程安全的，也不是不可变的。
    /// Please create a new instance of a state class for each operation to avoid race conditions and unexpected side effects.
    /// 请为每个操作创建一个状态类的新实例，以避免竞态条件和意外的副作用。
    /// </note>
    /// </remarks>
    /// 
    /// <threadsafety static="true" instance="true" />
    public class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly JobStorage _storage;
        private readonly IBackgroundJobFactory _factory;
        private readonly IBackgroundJobStateChanger _stateChanger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/>
        /// class with the storage from a global configuration.
        /// </summary>
        /// 
        /// <remarks>
        /// Please see the <see cref="GlobalConfiguration"/> class for the
        /// details regarding the global configuration.
        /// </remarks>
        public BackgroundJobClient()
            : this(JobStorage.Current)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/>
        /// class with the specified storage.
        /// </summary>
        /// 
        /// <param name="storage">Job storage to use for background jobs.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
        public BackgroundJobClient([NotNull] JobStorage storage)
            : this(storage, new BackgroundJobFactory(), new BackgroundJobStateChanger())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobClient"/> class
        /// with the specified storage, background job factory and state changer.
        /// </summary>
        /// 
        /// <param name="storage">Job storage to use for background jobs.</param>
        /// <param name="factory">Factory to create background jobs.</param>
        /// <param name="stateChanger">State changer to change states of background jobs.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stateChanger"/> is null.</exception>
        public BackgroundJobClient(
            [NotNull] JobStorage storage,
            [NotNull] IBackgroundJobFactory factory,
            [NotNull] IBackgroundJobStateChanger stateChanger)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (stateChanger == null) throw new ArgumentNullException(nameof(stateChanger));
            
            _storage = storage;
            _stateChanger = stateChanger;
            _factory = factory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public string Create(Job job, IState state)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (state == null) throw new ArgumentNullException(nameof(state));

            try
            {
                using (var connection = _storage.GetConnection())
                {
                    var context = new CreateContext(_storage, connection, job, state);
                    var backroundJob = _factory.Create(context);

                    return backroundJob?.Id;
                }
            }
            catch (Exception ex)
            {
                throw new BackgroundJobClientException("Background job creation failed. See inner exception for details.", ex);
            }
        }

        /// <inheritdoc />
        public bool ChangeState(string jobId, IState state, string expectedState)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (state == null) throw new ArgumentNullException(nameof(state));

            try
            {
                using (var connection = _storage.GetConnection())
                {
                    var appliedState = _stateChanger.ChangeState(new StateChangeContext(
                        _storage,
                        connection,
                        jobId,
                        state,
                        expectedState != null ? new[] { expectedState } : null));

                    return appliedState != null && appliedState.Name.Equals(state.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                throw new BackgroundJobClientException("State change of a background job failed. See inner exception for details", ex);
            }
        }
    }
}
