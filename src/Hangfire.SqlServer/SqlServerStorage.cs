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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
#if NETFULL
using System.Configuration;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;
#endif
using Dapper;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    /// <summary>
    /// SqlServer仓库
    /// </summary>
    public class SqlServerStorage : JobStorage
    {
        /// <summary>
        /// 已存在的数据库连接
        /// </summary>
        private readonly DbConnection _existingConnection;
        /// <summary>
        /// 数据仓库设置
        /// </summary>
        private readonly SqlServerStorageOptions _options;
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// 创建SqlServer仓库实例
        /// </summary>
        /// <param name="nameOrConnectionString">数据库连接字符串或连接配置名</param>
        public SqlServerStorage(string nameOrConnectionString)
            : this(nameOrConnectionString, new SqlServerStorageOptions())
        {
        }

        /// <summary>
        /// Initializes SqlServerStorage from the provided SqlServerStorageOptions and either the provided connection
        /// string or the connection string with provided name pulled from the application config file.
        /// 从提供的SqlServerStorageOptions和从应用程序配置文件中提取的提供的连接字符串或具有提供的名称的连接字符串初始化SqlServerStorage。
        /// </summary>
        /// <param name="nameOrConnectionString">
        /// Either a SQL Server connection string or the name of 
        /// a SQL Server connection string located in the connectionStrings node in the application config
        /// 要么是SQL Server连接字符串，要么是位于应用程序配置中的connectionStrings节点中的SQL Server连接字符串的名称
        /// </param>
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"><paramref name="nameOrConnectionString"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="nameOrConnectionString"/> argument is neither 
        /// a valid SQL Server connection string nor the name of a connection string in the application
        /// config file.</exception>
        public SqlServerStorage(string nameOrConnectionString, SqlServerStorageOptions options)
        {
            if (nameOrConnectionString == null) throw new ArgumentNullException(nameof(nameOrConnectionString));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _connectionString = GetConnectionString(nameOrConnectionString);
            _options = options;

            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with
        /// explicit instance of the <see cref="DbConnection"/> class that will be used
        /// to query the data.
        /// </summary>
        public SqlServerStorage([NotNull] DbConnection existingConnection)
            : this(existingConnection, new SqlServerStorageOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerStorage"/> class with
        /// explicit instance of the <see cref="DbConnection"/> class that will be used
        /// to query the data, with the given options.
        /// </summary>
        public SqlServerStorage([NotNull] DbConnection existingConnection, [NotNull] SqlServerStorageOptions options)
        {
            if (existingConnection == null) throw new ArgumentNullException(nameof(existingConnection));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _existingConnection = existingConnection;
            _options = options;

            Initialize();
        }

        public virtual PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        internal string SchemaName => _options.SchemaName;
        internal int? CommandTimeout => _options.CommandTimeout.HasValue ? (int)_options.CommandTimeout.Value.TotalSeconds : (int?)null;
        internal int? CommandBatchMaxTimeout => _options.CommandBatchMaxTimeout.HasValue ? (int)_options.CommandBatchMaxTimeout.Value.TotalSeconds : (int?)null;
        internal TimeSpan? SlidingInvisibilityTimeout => _options.SlidingInvisibilityTimeout;

        public override IMonitoringApi GetMonitoringApi()
        {
            return new SqlServerMonitoringApi(this, _options.DashboardJobListLimit);
        }

        public override IStorageConnection GetConnection()
        {
            return new SqlServerConnection(this);
        }

#pragma warning disable 618
        public override IEnumerable<IServerComponent> GetComponents()
#pragma warning restore 618
        {
            yield return new ExpirationManager(this, _options.JobExpirationCheckInterval);
            yield return new CountersAggregator(this, _options.CountersAggregateInterval);
        }

        public override void WriteOptionsToLog(ILog logger)
        {
            //logger.Info("Using the following options for SQL Server job storage:");
            logger.Info("对SQL Server作业存储使用以下选项:");
            //logger.Info($"    Queue poll interval: {_options.QueuePollInterval}.");
            logger.Info($"    排队轮询间隔: {_options.QueuePollInterval}.");
        }

        public override string ToString()
        {
            const string canNotParseMessage = "<Connection string can not be parsed>";

            try
            {
                var parts = _connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => new { Key = x[0].Trim(), Value = x[1].Trim() })
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                var builder = new StringBuilder();

                foreach (var alias in new[] { "Data Source", "Server", "Address", "Addr", "Network Address" })
                {
                    if (parts.ContainsKey(alias))
                    {
                        builder.Append(parts[alias]);
                        break;
                    }
                }

                if (builder.Length != 0) builder.Append("@");

                foreach (var alias in new[] { "Database", "Initial Catalog" })
                {
                    if (parts.ContainsKey(alias))
                    {
                        builder.Append(parts[alias]);
                        break;
                    }
                }

                return builder.Length != 0
                    ? $"SQL Server: {builder}"
                    : canNotParseMessage;
            }
            catch (Exception)
            {
                return canNotParseMessage;
            }
        }

        /// <summary>
        /// 确保数据库已连接
        /// </summary>
        /// <param name="dedicatedConnection"></param>
        /// <param name="action"></param>
        internal void UseConnection(DbConnection dedicatedConnection, [InstantHandle] Action<DbConnection> action)
        {
            UseConnection(dedicatedConnection, connection =>
            {
                action(connection);
                return true;
            });
        }

        /// <summary>
        /// 确保数据库已连接
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dedicatedConnection"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        internal T UseConnection<T>(DbConnection dedicatedConnection, [InstantHandle] Func<DbConnection, T> func)
        {
            DbConnection connection = null;

            try
            {
                connection = dedicatedConnection ?? CreateAndOpenConnection();
                return func(connection);
            }
            finally
            {
                if (dedicatedConnection == null)
                {
                    ReleaseConnection(connection);
                }
            }
        }

        /// <summary>
        /// 使用事务
        /// </summary>
        /// <param name="dedicatedConnection"></param>
        /// <param name="action"></param>
        internal void UseTransaction(DbConnection dedicatedConnection, [InstantHandle] Action<DbConnection, DbTransaction> action)
        {
            UseTransaction(dedicatedConnection, (connection, transaction) =>
            {
                action(connection, transaction);
                return true;
            }, null);
        }
        
        internal T UseTransaction<T>(
            DbConnection dedicatedConnection,
            [InstantHandle] Func<DbConnection, DbTransaction, T> func, 
            IsolationLevel? isolationLevel)
        {
#if NETFULL
            using (var transaction = CreateTransaction(isolationLevel ?? _options.TransactionIsolationLevel))
            {
                var result = UseConnection(dedicatedConnection, connection =>
                {
                    connection.EnlistTransaction(Transaction.Current);
                    return func(connection, null);
                });

                transaction.Complete();

                return result;
            }
#else
            return UseConnection(dedicatedConnection, connection =>
            {
                using (var transaction = connection.BeginTransaction(isolationLevel ?? IsolationLevel.ReadCommitted))
                {
                    var result = func(connection, transaction);
                    transaction.Commit();

                    return result;
                }
            });
#endif
        }

        /// <summary>
        /// 创建并打开数据库连接
        /// </summary>
        /// <returns></returns>
        internal DbConnection CreateAndOpenConnection()
        {
            var connection = _existingConnection ?? new SqlConnection(_connectionString);

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            return connection;
        }

        /// <summary>
        /// 判断是否是已存在的数据库连接
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal bool IsExistingConnection(IDbConnection connection)
        {
            return connection != null && ReferenceEquals(connection, _existingConnection);
        }

        /// <summary>
        /// 释放非已存在的数据库连接
        /// </summary>
        /// <param name="connection"></param>
        internal void ReleaseConnection(IDbConnection connection)
        {
            if (connection != null && !IsExistingConnection(connection))
            {
                connection.Dispose();
            }
        }

        /// <summary>
        /// 数据库连接初始化
        /// </summary>
        private void Initialize()
        {
            if (_options.PrepareSchemaIfNecessary)
            {
                UseConnection(null, connection =>
                {
                    SqlServerObjectsInstaller.Install(connection, _options.SchemaName);
                });
            }

            InitializeQueueProviders();
        }

        private void InitializeQueueProviders()
        {
            var defaultQueueProvider = new SqlServerJobQueueProvider(this, _options);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);
        }

        /// <summary>
        /// 获得数据库连接字符串
        /// </summary>
        /// <param name="nameOrConnectionString"></param>
        /// <returns></returns>
        private string GetConnectionString(string nameOrConnectionString)
        {
#if NETFULL
            if (IsConnectionString(nameOrConnectionString))
            {
                return nameOrConnectionString;
            }

            if (IsConnectionStringInConfiguration(nameOrConnectionString))
            {
                return ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }

            throw new ArgumentException(
                $"Could not find connection string with name '{nameOrConnectionString}' in application config file");
#else
            return nameOrConnectionString;
#endif
        }

#if NETFULL
        /// <summary>
        /// 判断是否是数据库连接字符串
        /// </summary>
        /// <param name="nameOrConnectionString"></param>
        /// <returns></returns>
        private bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }

        /// <summary>
        /// 判断是否是数据库连接配置名
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        private bool IsConnectionStringInConfiguration(string connectionStringName)
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSetting != null;
        }

        /// <summary>
        /// 创建事务
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        private TransactionScope CreateTransaction(IsolationLevel? isolationLevel)
        {
            return isolationLevel != null
                ? new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = isolationLevel.Value, Timeout = _options.TransactionTimeout })
                : new TransactionScope();
        }
#endif

        public static readonly DashboardMetric ActiveConnections = new DashboardMetric(
            "connections:active",
            "Metrics_ActiveConnections",
            page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, connection =>
                {
                    var sqlQuery = @"
select count(*) from sys.sysprocesses
where dbid = db_id(@name) and status != 'background' and status != 'sleeping'";

                    var value = connection
                        .Query<int>(sqlQuery, new { name = connection.Database })
                        .Single();

                    return new Metric(value);
                });
            });

        public static readonly DashboardMetric TotalConnections = new DashboardMetric(
            "connections:total",
            "Metrics_TotalConnections",
            page =>
            {
                var sqlStorage = page.Storage as SqlServerStorage;
                if (sqlStorage == null) return new Metric("???");

                return sqlStorage.UseConnection(null, connection =>
                {
                    var sqlQuery = @"
select count(*) from sys.sysprocesses
where dbid = db_id(@name) and status != 'background'";

                    var value = connection
                        .Query<int>(sqlQuery, new { name = connection.Database })
                        .Single();

                    return new Metric(value);
                });
            });
    }
}