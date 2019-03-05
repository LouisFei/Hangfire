using System;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Represents a job filter that <i>automatically deletes a background job</i>, when a certain amount of time elapsed since its creation. 
    /// 表示作业筛选器，该作业筛选器在创建作业后经过一定时间后自动删除后台作业。
    /// Deletion is taking place when a <see cref="Hangfire.Server.Worker"/> attempts to move a job to the <see cref="ProcessingState"/> state.
    /// 删除发生在Worker尝试将作业移动到ProcessingState状态。
    /// </summary>
    public sealed class LatencyTimeoutAttribute : JobFilterAttribute, IElectStateFilter
    {
        private readonly ILog _logger = LogProvider.For<LatencyTimeoutAttribute>();

        private readonly int _timeoutInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="LatencyTimeoutAttribute"/> class with the given timeout value.
        /// 使用给定的超时值初始化LatencyTimeoutAttribute类的新实例。
        /// </summary>
        /// <param name="timeoutInSeconds">
        /// Non-negative timeout value in seconds that will be used to determine whether to delete a job.
        /// 以秒为单位的非负超时值，用于确定是否删除作业。
        /// </param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="timeoutInSeconds"/> has a negative value.
        /// </exception>
        public LatencyTimeoutAttribute(int timeoutInSeconds)
        {
            if (timeoutInSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInSeconds), "Timeout value must be equal or greater than zero.");
            }

            _timeoutInSeconds = timeoutInSeconds;
            LogLevel = LogLevel.Debug;
        }

        /// <summary>
        /// Gets or sets a level for log message that will be produced, when a
        /// background job was deleted due to exceeded timeout.
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <inheritdoc />
        public void OnStateElection(ElectStateContext context)
        {
            var state = context.CandidateState as ProcessingState;
            if (state == null)
            {
                //this filter only accepts Processing
                return;
            }

            var elapsedTime = DateTime.UtcNow - context.BackgroundJob.CreatedAt;

            if (elapsedTime.TotalSeconds > _timeoutInSeconds)
            {
                context.CandidateState = new DeletedState
                {
                    Reason = $"Background job has exceeded latency timeout of {_timeoutInSeconds} second(s)"
                };

                _logger.Log(
                    LogLevel,
                    () => $"Background job '{context.BackgroundJob.Id}' has exceeded latency timeout of {_timeoutInSeconds} second(s) and will be deleted");
            }
        }

    }
}