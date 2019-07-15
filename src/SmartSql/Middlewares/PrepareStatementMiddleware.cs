﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using SmartSql.Configuration;
using SmartSql.Utils;
using System.Data.Common;
using SmartSql.TypeHandlers;
using Microsoft.Extensions.Logging;
using System.Data;
using SmartSql.Data;

namespace SmartSql.Middlewares
{
    public class PrepareStatementMiddleware : AbstractMiddleware
    {
        private readonly ILogger _logger;
        private readonly SqlParamAnalyzer _sqlParamAnalyzer;
        private readonly DbProviderFactory _dbProviderFactory;
        private readonly TypeHandlerFactory _typeHandlerFactory;

        public PrepareStatementMiddleware(SmartSqlConfig smartSqlConfig)
        {
            _logger = smartSqlConfig.LoggerFactory.CreateLogger<PrepareStatementMiddleware>();
            _sqlParamAnalyzer = smartSqlConfig.SqlParamAnalyzer;
            _dbProviderFactory = smartSqlConfig.Database.DbProvider.Factory;
            _typeHandlerFactory = smartSqlConfig.TypeHandlerFactory;
        }

        public override void Invoke<TResult>(ExecutionContext executionContext)
        {
            InitParameters(executionContext);
            InvokeNext<TResult>(executionContext);
        }

        #region Init Parameter

        private void InitParameters(ExecutionContext executionContext)
        {
            BuildSql(executionContext.Request);
            BuildDbParameters(executionContext.Request);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(executionContext.FormatSql(true));
            }
        }

        private void BuildDbParameters(AbstractRequestContext reqConetxt)
        {
            var dbParameterNames = _sqlParamAnalyzer.Analyse(reqConetxt.RealSql);
            if (reqConetxt.CommandType == CommandType.StoredProcedure)
            {
                foreach (var sqlParameter in reqConetxt.Parameters.Values)
                {
                    var sourceParam = _dbProviderFactory.CreateParameter();
                    sourceParam.ParameterName = sqlParameter.Name;
                    sourceParam.Value = sqlParameter.Value;
                    sqlParameter.SourceParameter = sourceParam;
                    InitSourceDbParameter(sourceParam, sqlParameter);
                }
            }
            else
            {
                foreach (var paramName in dbParameterNames)
                {
                    var parameter = reqConetxt.ParameterMap?.GetParameter(paramName);
                    var propertyName = paramName;
                    ITypeHandler typeHandler = null;
                    if (parameter != null)
                    {
                        propertyName = parameter.Property;
                        typeHandler = parameter.Handler;
                    }

                    if (!reqConetxt.Parameters.TryGetValue(propertyName, out var sqlParameter))
                    {
                        continue;
                    }

                    var sourceParam = _dbProviderFactory.CreateParameter();
                    sourceParam.ParameterName = paramName;

                    if (typeHandler == null)
                    {
                        typeHandler = sqlParameter.TypeHandler ??
                                      _typeHandlerFactory.GetTypeHandler(sqlParameter.ParameterType);
                    }

                    typeHandler.SetParameter(sourceParam, sqlParameter.Value);
                    sqlParameter.SourceParameter = sourceParam;
                    InitSourceDbParameter(sourceParam, sqlParameter);
                }
            }
        }

        private void InitSourceDbParameter(DbParameter sourceParam, SqlParameter sqlParameter)
        {
            if (sqlParameter.DbType.HasValue)
            {
                sourceParam.DbType = sqlParameter.DbType.Value;
            }

            if (sqlParameter.Direction.HasValue)
            {
                sourceParam.Direction = sqlParameter.Direction.Value;
            }

            if (sqlParameter.Precision.HasValue)
            {
                sourceParam.Precision = sqlParameter.Precision.Value;
            }

            if (sqlParameter.Scale.HasValue)
            {
                sourceParam.Scale = sqlParameter.Scale.Value;
            }

            if (sqlParameter.Size.HasValue)
            {
                sourceParam.Size = sqlParameter.Size.Value;
            }
        }

        private void BuildSql(AbstractRequestContext requestContext)
        {
            if (!requestContext.IsStatementSql)
            {
                return;
            }

            requestContext.SqlBuilder = new StringBuilder();
            requestContext.Statement.BuildSql(requestContext);
            requestContext.RealSql = requestContext.SqlBuilder.ToString().Trim();
        }

        #endregion

        public override async Task InvokeAsync<TResult>(ExecutionContext executionContext)
        {
            InitParameters(executionContext);
            await InvokeNextAsync<TResult>(executionContext);
        }
    }
}