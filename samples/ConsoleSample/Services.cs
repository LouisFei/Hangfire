using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;

namespace ConsoleSample
{
    public class Services
    {
        private static readonly Random Rand = new Random();

        public void EmptyDefault()
        {
        }

        /// <summary>
        /// 可取消的异步任务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Async(CancellationToken cancellationToken)
        {
            //创建异步产生当前上下文的等待任务。
            //等待时，上下文将异步转换回等待时的当前上下文。
            //当任务比较耗时时，中断以分割成多个小的任务片断执行。
            await Task.Yield();

            //创建一个在指定的时间间隔后完成的可取消任务。
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }

        /*
            设置任务队列
            对于非周期任务，只需要在执行的方法添加Queue特性就能指定该任务让特定的队列服务器处理。
            而周期任务，则需要先声明。
        */
        /// <summary>
        /// 
        /// </summary>
        [Queue("critical")] //设定队列名称
        public void EmptyCritical()
        {
        }

        /// <summary>
        /// 一个会报错的任务，设定了重试次数，和超时时间。
        /// </summary>
        [AutomaticRetry(Attempts = 0), LatencyTimeout(30)]
        public void Error()
        {
            Console.WriteLine("Beginning error task...");
            throw new InvalidOperationException(null, new FileLoadException());
        }

        [Queue("critical")]
        public void Random(int number)
        {
            int time;
            lock (Rand)
            {
                time = Rand.Next(10);
            }

            if (time < 5)
            {
                throw new Exception();
            }

            Thread.Sleep(TimeSpan.FromSeconds(5 + time));
            Console.WriteLine("Finished task: " + number);
        }

        public void Cancelable(int iterationCount, IJobCancellationToken token)
        {
            try
            {
                for (var i = 1; i <= iterationCount; i++)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Performing step {0} of {1}...", i, iterationCount);

                    token.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancellation requested, exiting...");
                throw;
            }
        }

        [DisplayName("Name: {0}")]
        public void Args(string name, int authorId, DateTime createdAt)
        {
            Console.WriteLine($"{name}, {authorId}, {createdAt}");
        }

        public void Custom(int id, string[] values, CustomObject objects, DayOfWeek dayOfWeek)
        {
        }

        public void FullArgs(
            bool b,
            int i,
            char c,
            DayOfWeek e,
            string s,
            TimeSpan t,
            DateTime d,
            CustomObject o,
            string[] sa,
            int[] ia,
            long[] ea,
            object[] na,
            List<string> sl)
        {
        }

        public class CustomObject
        {
            public int Id { get; set; }
            public CustomObject[] Children { get; set; }
        }

        public void Write(char character)
        {
            Console.Write(character);
        }

        public void WriteBlankLine()
        {
            Console.WriteLine();
        }
    }
}