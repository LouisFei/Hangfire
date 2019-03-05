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
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using Hangfire.Logging;

namespace Hangfire.SqlServer
{
    /// <summary>
    /// SqlServer对象安装器
    /// </summary>
    public static class SqlServerObjectsInstaller
    {
        public static readonly int RequiredSchemaVersion = 5;
        /// <summary>
        /// 重试次数
        /// </summary>
        private const int RetryAttempts = 3;

        public static void Install(DbConnection connection)
        {
            Install(connection, null);
        }

        /// <summary>
        /// 安装数据库存储表
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="schema"></param>
        public static void Install(DbConnection connection, string schema)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var log = LogProvider.GetLogger(typeof(SqlServerObjectsInstaller));

            //log.Info("Start installing Hangfire SQL objects...");
            log.Info("开始安装Hangfire SQL对象…");

            //创建SqlServer数据库及表的脚本，作为了程序集内嵌资源。
            //下面的代码把它读取出来。
            var script = GetStringResource(
                typeof(SqlServerObjectsInstaller).GetTypeInfo().Assembly, 
                "Hangfire.SqlServer.Install.sql");

            script = script.Replace("SET @TARGET_SCHEMA_VERSION = 5;", "SET @TARGET_SCHEMA_VERSION = " + RequiredSchemaVersion + ";");

            script = script.Replace("$(HangFireSchema)", !string.IsNullOrWhiteSpace(schema) ? schema : Constants.DefaultSchema);

#if NETFULL
            for (var i = 0; i < RetryAttempts; i++)
            {
                try
                {
                    connection.Execute(script, commandTimeout: 0);
                    break;
                }
                catch (DbException ex)
                {
                    if (ex.ErrorCode == 1205)
                    {
                        log.WarnException("Deadlock occurred during automatic migration execution. Retrying...", ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
#else
            connection.Execute(script, commandTimeout: 0);
#endif

            //log.Info("Hangfire SQL objects installed.");
            log.Info("安装了Hangfire SQL对象。");
        }

        #region GetStringResource
        /// <summary>
        /// 获取程序集内嵌的文本资源，返回字符串形式。
        /// </summary>
        /// <param name="assembly">程序集</param>
        /// <param name="resourceName">资源名</param>
        /// <returns></returns>
        private static string GetStringResource(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) 
                {
                    throw new InvalidOperationException(
                        $"Requested resource `{resourceName}` was not found in the assembly `{assembly}`.");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        #endregion
    }
}
