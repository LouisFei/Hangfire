using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;

namespace ConsoleSample
{
    public static class Program
    {
        public static int ToInt(this string str, int defValue)
        {
            int.TryParse(str, out defValue);
            return defValue;
        }

        public static void Main()
        {
            GlobalConfiguration.Configuration
                .UseColouredConsoleLogProvider()
                .UseSqlServerStorage(@"server=.;database=Hangfire;uid=hf;pwd=hf123;");
            //.UseMsmqQueues(@".\Private$\hangfire{0}", "default", "critical");

            //RecurringJob.AddOrUpdate(() => Console.WriteLine("Hello, world!"), Cron.Minutely);
            //RecurringJob.AddOrUpdate("hourly", () => Console.WriteLine("Hello"), "25 15 * * *");
            //RecurringJob.AddOrUpdate("Hawaiian", () => Console.WriteLine("Hawaiian"),  "15 08 * * *", TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time"));
            //RecurringJob.AddOrUpdate("UTC", () => Console.WriteLine("UTC"), "15 18 * * *");
            //RecurringJob.AddOrUpdate("Russian", () => Console.WriteLine("Russian"), "15 21 * * *", TimeZoneInfo.Local);

            var options = new BackgroundJobServerOptions
            {
                Queues = new[] { "critical", "default" }, //队列名称，只能小写字母
                //WorkerCount = Environment.ProcessorCount * 5, //并发任务数
                //ServerName = "hangfire1", //服务器名称
            };
            /*
                Queues要处理的队列列表
                对于多个服务器同时连接到数据库，Hangfire会认为他们是分布式中的一份子。
                现实中不同服务器往往存在着差异，这个时候就需要合理配置服务器（应用）的处理队列。
                例如：
                    对于服务器性能差异的处理，有100个A任务和50个B任务需要处理，假设A服务器的性能是B服务器的两倍，如果不配置队列，那么会平分任务给两个服务器。
                    如果我们只让B服务器处理B任务，而A服务器同时处理两种任务，这样B就能减少一些压力。
                
                WorkerCount并发任务数，超出并发数将等待之前的任务完成。
                默认的并发任务数是线程（cpu）的5倍，如果IO密集型任务多而CPU密集型的任务少，可以考虑调高并发任务数。

             */

            /*
                任务类型
                    Fire-and-forget 直接将任务加入到待执行任务队列。
                    Delayed 在当前时间后的某个时间将任务加入到待执行任务队列。
                    Recurring 周期性任务，每一个周期就将任务加入到待执行任务队列。
                    Continuations 继续执行任务。
             */

            
            using (new BackgroundJobServer(options))
            {
                var count = 1;

                while (true)
                {
                    var command = Console.ReadLine();
                    var workCount = 1;
                    var cmds = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    command = cmds[0];
                    if (cmds.Length > 1)
                    {
                        workCount = cmds[1].ToInt(1);
                    }

                    if (command == null || command.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    _add(command, workCount);

                    _async(command, workCount);

                    _static(command, workCount);

                    _error(command, workCount);

                    _args(command, workCount);

                    _custom(command, workCount);

                    _fullargs(command, workCount);

                    _in(command, ref count, workCount);

                    _cancelable(command, workCount);

                    _delete(command, workCount);

                    _fast(command, workCount);

                    _generic(command);

                    _continuations(command);
                }
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// 对象实例方法任务
        /// </summary>
        /// <param name="command"></param>
        private static void _add(string command, int workCount)
        {
            if (command.StartsWith("add", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    //var workCount = int.Parse(command.Substring(4));
                    for (var i = 0; i < workCount; i++)
                    {
                        var number = i;
                        BackgroundJob.Enqueue<Services>(x => x.Random(number)); //Fire-and-forget 直接将任务加入到待执行任务队列。
                    }
                    Console.WriteLine($"{DateTime.Now} Jobs enqueued.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 异步耗时任务
        /// </summary>
        /// <param name="command"></param>
        private static void _async(string command, int workCount)
        {
            if (command.StartsWith("async", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    //var workCount = int.Parse(command.Substring(6));
                    for (var i = 0; i < workCount; i++)
                    {
                        BackgroundJob.Enqueue<Services>(x => x.Async(CancellationToken.None));
                    }
                    Console.WriteLine("Jobs enqueued.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 静态任务
        /// </summary>
        /// <param name="command"></param>
        private static void _static(string command, int workCount)
        {
            if (command.StartsWith("static", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    //var workCount = int.Parse(command.Substring(7));
                    for (var i = 0; i < workCount; i++)
                    {
                        BackgroundJob.Enqueue(() => Console.WriteLine("Hello, {0}!", "world"));
                    }
                    Console.WriteLine("Jobs enqueued.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 模拟任务报错
        /// </summary>
        /// <param name="command"></param>
        private static void _error(string command, int workCount)
        {
            if (command.StartsWith("error", StringComparison.OrdinalIgnoreCase))
            {
                //var workCount = int.Parse(command.Substring(6));
                for (var i = 0; i < workCount; i++)
                {
                    BackgroundJob.Enqueue<Services>(x => x.Error());
                }
            }
        }

        /// <summary>
        /// 带参数的任务
        /// </summary>
        /// <param name="command"></param>
        private static void _args(string command, int workCount)
        {
            if (command.StartsWith("args", StringComparison.OrdinalIgnoreCase))
            {
                //var workCount = int.Parse(command.Substring(5));
                for (var i = 0; i < workCount; i++)
                {
                    BackgroundJob.Enqueue<Services>(x => x.Args(Guid.NewGuid().ToString(), 14442, DateTime.UtcNow));
                }
            }
        }

        /// <summary>
        /// 复杂参数的任务
        /// </summary>
        /// <param name="command"></param>
        private static void _custom(string command, int workCount)
        {
            if (command.StartsWith("custom", StringComparison.OrdinalIgnoreCase))
            {
                //var workCount = int.Parse(command.Substring(7));
                for (var i = 0; i < workCount; i++)
                {
                    BackgroundJob.Enqueue<Services>(x => x.Custom(
                        new Random().Next(),
                        new[] { "Hello", "world!" },
                        new Services.CustomObject { Id = 123 },
                        DayOfWeek.Friday
                        ));
                }
            }
        }

        /// <summary>
        /// 再复杂参数点的任务
        /// </summary>
        /// <param name="command"></param>
        private static void _fullargs(string command, int workCount)
        {
            if (command.StartsWith("fullargs", StringComparison.OrdinalIgnoreCase))
            {
                //var workCount = int.Parse(command.Substring(9));
                for (var i = 0; i < workCount; i++)
                {
                    BackgroundJob.Enqueue<Services>(x => x.FullArgs(
                        false,
                        123,
                        'c',
                        DayOfWeek.Monday,
                        "hello",
                        new TimeSpan(12, 13, 14),
                        new DateTime(2012, 11, 10),
                        new Services.CustomObject { Id = 123 },
                        new[] { "1", "2", "3" },
                        new[] { 4, 5, 6 },
                        new long[0],
                        null,
                        new List<string> { "7", "8", "9" }));
                }
            }
        }

        /// <summary>
        /// 可以使用in参数的任务
        /// </summary>
        /// <param name="command"></param>
        /// <param name="count"></param>
        private static void _in(string command, ref int count, int seconds)
        {
            if (command.StartsWith("in", StringComparison.OrdinalIgnoreCase))
            {
                //var seconds = int.Parse(command.Substring(2));
                var number = count++;
                BackgroundJob.Schedule<Services>(x => x.Random(number), TimeSpan.FromSeconds(seconds));
            }
        }

        /// <summary>
        /// 可以被取消的任务
        /// </summary>
        /// <param name="command"></param>
        private static void _cancelable(string command, int iterations)
        {
            if (command.StartsWith("cancelable", StringComparison.OrdinalIgnoreCase))
            {
                //var iterations = int.Parse(command.Substring(11));
                BackgroundJob.Enqueue<Services>(x => x.Cancelable(iterations, JobCancellationToken.Null));
            }
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="command"></param>
        private static void _delete(string command, int workCount)
        {
            if (command.StartsWith("delete", StringComparison.OrdinalIgnoreCase))
            {
                //var workCount = int.Parse(command.Substring(7));
                for (var i = 0; i < workCount; i++)
                {
                    var jobId = BackgroundJob.Enqueue<Services>(x => x.EmptyDefault());
                    BackgroundJob.Delete(jobId);
                }
            }
        }

        /// <summary>
        /// 快速插入任务
        /// </summary>
        /// <param name="command"></param>
        private static void _fast(string command, int workCount)
        {
            if (command.StartsWith("fast", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    //var workCount = int.Parse(command.Substring(5));
                    Parallel.For(0, workCount, i =>
                    {
                        if (i % 2 == 0)
                        {
                            BackgroundJob.Enqueue<Services>(x => x.EmptyCritical());
                        }
                        else
                        {
                            BackgroundJob.Enqueue<Services>(x => x.EmptyDefault());
                        }
                    });
                    Console.WriteLine("Jobs enqueued.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 泛型任务
        /// </summary>
        /// <param name="command"></param>
        private static void _generic(string command)
        {
            if (command.StartsWith("generic", StringComparison.OrdinalIgnoreCase))
            {
                BackgroundJob.Enqueue<GenericServices<string>>(x => x.Method("hello", 1));
            }
        }

        /// <summary>
        /// 串行任务
        /// </summary>
        /// <param name="command"></param>
        private static void _continuations(string command)
        {
            if (command.StartsWith("continuations", StringComparison.OrdinalIgnoreCase))
            {
                WriteString("Hello, Hangfire continuations!");
            }
        }


        /// <summary>
        /// 写字符串任务，把字符分拆到子任务中，串行执行。
        /// </summary>
        /// <param name="value"></param>
        public static void WriteString(string value)
        {
            var lastId = BackgroundJob.Enqueue<Services>(x => x.Write(value[0]));

            for (var i = 1; i < value.Length; i++)
            {
                lastId = BackgroundJob.ContinueWith<Services>(lastId, x => x.Write(value[i]));
            }

            BackgroundJob.ContinueWith<Services>(lastId, x => x.WriteBlankLine());
        }
    }
}
