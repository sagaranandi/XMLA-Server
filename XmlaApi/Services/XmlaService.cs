using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AnalysisServices.AdomdClient;
using DuckDB.NET.Data;
using Microsoft.Extensions.Logging;
using XmlaApi.Models;


namespace XmlaApi.Services
{
    public class XmlaService
{
    private readonly ILogger<XmlaService> _logger;
    private AdomdConnection _connection;

    public XmlaService(ILogger<XmlaService> logger)
    {
        _logger = logger;
    }

    public bool Load(LoadRequest request) 
    {
        _logger.LogInformation("Connecting to Workspace: {Workspace}, Dataset: {DatasetName}", request.Workspace, request.DatasetName);

        try
        {
            if (request == null || string.IsNullOrEmpty(request.Workspace) || string.IsNullOrEmpty(request.DatasetName))
            {
                throw new ArgumentException("Workspace and DatasetName must be provided.");
            }

            string connectionString = $"Data Source={request.Workspace};Initial Catalog={request.DatasetName};";
            _logger.LogInformation("Connection String: {connectionString}", connectionString);
            _connection = new AdomdConnection(connectionString);
            _logger.LogInformation("Opening connection...");
            _connection.Open();
            _logger.LogInformation("Connection opened successfully.");

            return true;
        }
        catch (Exception ex)
        {
            LogDetailedException(ex, "Connection error");
            return false;
        }
    }

    public string Execute(string daxQuery)
    {
        try
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Connection is not established. Call Load first.");
            }

            using var command = new AdomdCommand(daxQuery, _connection);
            using var reader = command.ExecuteReader();

            var results = new List<Dictionary<string, object>>();
            int pageSize = 1000;
            int pageCount = 0;

            using var duckDbConnection = new DuckDBConnection("DataSource=:memory:");
            duckDbConnection.Open();

            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(row);

                if (results.Count >= pageSize)
                {
                    string parquetPath = ExportToParquet(results, pageCount, duckDbConnection);
                    _logger.LogInformation($"Generated Parquet file: {parquetPath}");
                    results.Clear();
                    pageCount++;
                }
            }

            if (results.Count > 0)
            {
                string parquetPath = ExportToParquet(results, pageCount, duckDbConnection);
                _logger.LogInformation($"Generated Parquet file: {parquetPath}");
            }

            return $"path/to/parquet/files/page_{pageCount}.parquet";
        }
        catch (Exception ex)
        {
            LogDetailedException(ex, "Error executing DAX query in Execute method");
            return null;
        }
    }

    private string ExportToParquet(List<Dictionary<string, object>> queryResult, int page, DuckDBConnection duckDbConnection)
    {
        try
        {
            var dataTable = new DataTable();
            foreach (var column in queryResult[0].Keys)
            {
                dataTable.Columns.Add(column);
            }

            foreach (var row in queryResult)
            {
                var dataRow = dataTable.NewRow();
                foreach (var column in row.Keys)
                {
                    dataRow[column] = row[column];
                }
                dataTable.Rows.Add(dataRow);
            }

            string parquetPath = $"path/to/parquet/files/page_{page}.parquet";
            // Create a temporary table to insert data before exporting
            using var createTableCommand = duckDbConnection.CreateCommand();
            createTableCommand.CommandText = "CREATE TABLE temp_table AS SELECT * FROM dataTable;";
            createTableCommand.ExecuteNonQuery();

            using var exportCommand = duckDbConnection.CreateCommand();
            exportCommand.CommandText = $"EXPORT (SELECT * FROM temp_table) TO '{parquetPath}' (FORMAT PARQUET);";
            exportCommand.ExecuteNonQuery();

            return parquetPath;
        }
        catch (Exception ex)
        {
            LogDetailedException(ex, "Error exporting to Parquet in ExportToParquet method");
            return null;
        }
    }

    private void LogDetailedException(Exception ex, string message)
    {
        _logger.LogError(ex, message);
        var innerException = ex.InnerException;
        while (innerException != null)
        {
            _logger.LogError(innerException, "Inner Exception: {Message}", innerException.Message);
            innerException = innerException.InnerException;
        }
    }
}

}
