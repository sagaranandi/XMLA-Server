using System;
using System.Data;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Extensions.Logging;
using XmlaApi.Models;

namespace XmlaApi.Services
{
    public class XmlaService
    {
        private readonly ILogger<XmlaService> _logger;
        private AdomdConnection _connection;
        private readonly string _powerBiDomainURL;

        public XmlaService(ILogger<XmlaService> logger, string powerBiDomainURL)
        {
            _logger = logger;
            _powerBiDomainURL = powerBiDomainURL;
        }

        //public async Task HandleWebSocketAsync(WebSocket webSocket)
        //{
        //    var buffer = new byte[1024 * 4];

        //    while (webSocket.State == WebSocketState.Open)
        //    {
        //        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //        if (result.MessageType == WebSocketMessageType.Close)
        //        {
        //            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
        //            break;
        //        }

        //        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

        //        // Deserialize the WebSocket message to determine the request type and payload
        //        var action = JsonSerializer.Deserialize<WebSocketRequest>(message);

        //        if (action != null)
        //        {
        //            var bearerToken = action.BearerToken; // Get Bearer Token from action model

        //            switch (action.Type)
        //            {
        //                case "Load":
        //                    // Deserialize Payload as LoadRequest
        //                    var loadPayload = (JsonElement)action.Payload;
        //                    var loadRequest = JsonSerializer.Deserialize<LoadRequest>(loadPayload.GetRawText());
        //                    bool loadSuccess = await Load(loadRequest, bearerToken);
        //                    await SendMessageAsync(webSocket, loadSuccess ? "Load succeeded" : "Load failed");
        //                    break;

        //                case "Execute":
        //                    // Deserialize Payload as ExecuteDax
        //                    var executePayload = (JsonElement)action.Payload;
        //                    var executeRequest = JsonSerializer.Deserialize<ExecuteDax>(executePayload.GetRawText());
        //                    var executeResponse = Execute(executeRequest, webSocket);
        //                    await SendMessageAsync(webSocket, executeResponse ?? "Execution failed");
        //                    break;

        //                default:
        //                    await SendMessageAsync(webSocket, "Unknown action type");
        //                    break;
        //            }
        //        }



        //    }
        //}

        public async Task<bool> Load(LoadRequest request, string bearerToken)
        {
            _logger.LogInformation("Connecting to Workspace: {Workspace}, Dataset: {DatasetName}", request.Workspace, request.DatasetName);
            try
            {
                if (string.IsNullOrEmpty(request.Workspace) || string.IsNullOrEmpty(request.DatasetName) || string.IsNullOrEmpty(bearerToken))
                    throw new ArgumentException("Workspace, DatasetName, and BearerToken must be provided.");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);


                var response = await client.PostAsync($"{_powerBiDomainURL}v1/user/auth/powerbi/refresh/", null);
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Power BI token response: StatusCode={StatusCode}, Reason={Reason}, Content={Content}", response.StatusCode, response.ReasonPhrase, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get Power BI token. Response content: {Content}", responseContent);
                    return false;
                }

                // Process the response content to extract the token
                try
                {
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Payload))
                    {
                        _logger.LogError("Invalid or empty payload received.");
                        return false;
                    }

                    // Decode the token payload
                    var base64Payload = tokenResponse.Payload.Split('.')[1];
                    base64Payload = base64Payload.PadRight(base64Payload.Length + (4 - base64Payload.Length % 4) % 4, '=').Replace('-', '+').Replace('_', '/');

                    try
                    {
                        var decodedBytes = Convert.FromBase64String(base64Payload);
                        var decodedPayload = Encoding.UTF8.GetString(decodedBytes);

                        _logger.LogInformation("Decoded payload: {DecodedPayload}", decodedPayload);
                        //var payloadData = JsonSerializer.Deserialize<PayloadData>(decodedPayload);

                        // Use the token from the response for establishing a connection
                        var token = tokenResponse.Payload;
                        var connectionString = $"Data Source={request.Workspace}; Initial Catalog={request.DatasetName}; Password={token}";

                        _connection = new AdomdConnection(connectionString);
                        _connection.Open();
                        _logger.LogInformation("Connection opened successfully.");
                        return true;
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogError("Error decoding payload: {Message}", ex.Message);
                        return false;
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError("Error deserializing token response: {Message}", jsonEx.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogDetailedException(ex, "Connection error");
                return false;
            }
        }


        //websocket approach
        //public string Execute(ExecuteDax request, WebSocket webSocket)
        //{
        //    try
        //    {
        //        if (_connection == null || _connection.State != ConnectionState.Open)
        //            throw new InvalidOperationException("Connection is not established. Call Load first.");

        //        var daxQuery = request.DaxQuery;
        //        using var command = new AdomdCommand(daxQuery, _connection);

        //        var stopwatch = Stopwatch.StartNew();
        //        _logger.LogInformation("Executing DAX query...");
        //        using var reader = command.ExecuteReader();

        //        int totalRowCount = 0;
        //        int batchSize = 5000;
        //        List<Dictionary<string, object>> batch = new List<Dictionary<string, object>>();

        //        while (reader.Read())
        //        {
        //            totalRowCount++;

        //            // Prepare the current row as a dictionary of column names and values
        //            Dictionary<string, object> row = new Dictionary<string, object>();
        //            for (int i = 0; i < reader.FieldCount; i++)
        //            {
        //                var columnName = reader.GetName(i) ?? $"Column{i}";
        //                var columnValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
        //                row[columnName] = columnValue;
        //            }

        //            // Add the row to the current batch
        //            batch.Add(row);

        //            // When batch size is reached, send the batch as a JSON array
        //            if (batch.Count >= batchSize)
        //            {
        //                SendBatchAsync(webSocket, batch, totalRowCount).Wait();
        //                batch.Clear();
        //            }
        //        }

        //        // Send any remaining rows in the last batch
        //        if (batch.Count > 0)
        //        {
        //            SendBatchAsync(webSocket, batch, totalRowCount).Wait();
        //        }

        //        stopwatch.Stop();
        //        _logger.LogInformation($"DAX query executed. Total rows processed: {totalRowCount}. Time taken: {stopwatch.ElapsedMilliseconds} ms");
        //        SendMessageAsync(webSocket, $"Query executed successfully. Total rows processed: {totalRowCount}. Time taken: {stopwatch.ElapsedMilliseconds} ms").Wait();

        //        return $"Query executed successfully. Time taken: {stopwatch.ElapsedMilliseconds} ms";
        //    }
        //    catch (Exception ex)
        //    {
        //        LogDetailedException(ex, "Error executing DAX query in Execute method");
        //        return null;
        //    }
        //}

        // Helper method to send a batch as JSON over WebSocket

        public async Task<List<Dictionary<string, object>>> Execute(ExecuteDax request)
        {
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Connection is not established. Call Load first.");

                var daxQuery = request.DaxQuery;
                _logger.LogInformation("DAX QUERY: {query}", request.DaxQuery);
                using var command = new AdomdCommand(daxQuery, _connection);

                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("Executing DAX query...");
                using var reader = command.ExecuteReader();

                var resultList = new List<Dictionary<string, object>>();

                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i) ?? $"Column{i}";
                        var columnValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[columnName] = columnValue;
                    }
                    resultList.Add(row);
                }

                stopwatch.Stop();
                _logger.LogInformation($"DAX query executed. Total rows processed: {resultList.Count}. Time taken: {stopwatch.ElapsedMilliseconds} ms");

                return resultList;
            }
            catch (Exception ex)
            {
                LogDetailedException(ex, "Error executing DAX query in Execute method");
                return null;
            }
        }

        private async Task SendBatchAsync(WebSocket webSocket, List<Dictionary<string, object>> batch, int totalRowCount)
        {
            // Convert the batch to JSON
            string jsonBatch = JsonSerializer.Serialize(batch);

            // Send the JSON data as a single message
            await SendMessageAsync(webSocket, $"Batch up to row {totalRowCount}: {jsonBatch}");
        }


        private async Task SendMessageAsync(WebSocket webSocket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
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
