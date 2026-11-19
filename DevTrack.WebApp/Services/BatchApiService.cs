using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Shared;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DevTrack.WebApp.Services;

public class BatchApiService : IBatchService
{
    private readonly IBatchApiClient apiClient;
    private readonly ILogger<BatchApiService> logger;

    public BatchApiService(IBatchApiClient api, ILogger<BatchApiService> logger)
    {
        apiClient = api;
        this.logger = logger;
    }

    public async Task<PagedResult<BatchResponse>> GetBatchesAsync(PaginationRequest request)
    {
        return await apiClient.GetBatchesAsync(request);
    }
    public async Task<Result<BatchResponse>> GetBatchByIdAsync(int id)
    {
        return await apiClient.GetBatchByIdAsync(id);
    }

    public async Task<Result<BatchResponse>> CreateBatchAsync(BatchRequest request)
    {
        logger.LogInformation(
            "CreateBatchAsync => Request started for BatchName={BatchName}, StartDate={StartDate}, EndDate={EndDate}",
            request.BatchName, request.StartDate, request.EndDate);

        try
        {
            var result = await apiClient.CreateBatchAsync(request);
            logger.LogInformation(
                "CreateBatchAsync => Request completed with IsSuccess={IsSuccess}, Message={Message}",
                result.IsSuccess, result.Message);
            return result;
        }
        catch (HttpRequestException ex)
        {
            var apiMessage = GetReadableApiError(ex.Message);
            logger.LogError(ex,
                "CreateBatchAsync => API exception. BatchName={BatchName}, StatusCode={StatusCode}, ApiMessage={ApiMessage}",
                request.BatchName, ex.StatusCode, apiMessage);
            return Result<BatchResponse>.Failure(apiMessage);
        }
    }

    public async Task<Result<List<BatchAssignmentModel>>> GetBatchDevelopersAsync(int batchId)
    {
        return await apiClient.GetBatchDevelopersAsync(batchId);
    }
    public async Task<Result> UpdateBatchAssignmentsAsync(int batchId, List<int> selectedDeveloperIds)
    {
        return await apiClient.UpdateBatchAssignmentsAsync(batchId, selectedDeveloperIds);
    }

    private static string GetReadableApiError(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return "API validation failed.";
        }

        try
        {
            using var document = JsonDocument.Parse(rawMessage);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var firstError = errors.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
                if (!string.IsNullOrWhiteSpace(firstError))
                {
                    return firstError;
                }
            }

            if (root.TryGetProperty("title", out var title) && title.GetString() is { Length: > 0 } titleText)
            {
                return titleText;
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            // API errors can be plain text or JSON outside the validation-error schema.
        }

        return rawMessage;
    }
}
