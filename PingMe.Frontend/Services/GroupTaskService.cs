using System.Net.Http.Json;
using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class GroupTaskService : ApiService
{
    public string? LastError { get; private set; }

    public GroupTaskService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<GroupTaskListResponse?> GetAsync(GroupTaskQuery request)
    {
        var query = new List<string>
        {
            $"page={request.Page}",
            $"pageSize={request.PageSize}"
        };

        if (request.GroupId.HasValue)
            query.Add($"groupId={request.GroupId.Value}");

        if (!string.IsNullOrWhiteSpace(request.Keyword))
            query.Add($"keyword={Uri.EscapeDataString(request.Keyword)}");

        if (!string.IsNullOrWhiteSpace(request.Status))
            query.Add($"status={Uri.EscapeDataString(request.Status)}");

        if (!string.IsNullOrWhiteSpace(request.Priority))
            query.Add($"priority={Uri.EscapeDataString(request.Priority)}");

        if (request.AssignedToMe)
            query.Add("assignedToMe=true");

        if (request.Overdue)
            query.Add("overdue=true");

        return await GetFromJsonAsync<GroupTaskListResponse>($"tasks?{string.Join("&", query)}");
    }

    public async Task<int> GetOverdueCountAsync()
    {
        var response = await GetFromJsonAsync<CountResponse>("tasks/overdue-count");
        return Math.Max(0, response?.Count ?? 0);
    }

    public async Task<GroupTaskResponse?> GetByIdAsync(int id)
        => await GetFromJsonAsync<GroupTaskResponse>($"tasks/{id}");

    public async Task<GroupTaskResponse?> CreateAsync(CreateGroupTaskRequest request)
    {
        LastError = null;
        var response = await PostAsync("tasks", request);
        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadErrorAsync(response);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GroupTaskResponse>();
    }

    public async Task<GroupTaskResponse?> UpdateAsync(int id, UpdateGroupTaskRequest request)
    {
        LastError = null;
        var response = await PutAsync($"tasks/{id}", request);
        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadErrorAsync(response);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GroupTaskResponse>();
    }

    public async Task<GroupTaskResponse?> CompleteAsync(int id, bool isCompleted)
    {
        LastError = null;
        var response = await PatchAsync($"tasks/{id}/complete", new CompleteGroupTaskRequest { IsCompleted = isCompleted });
        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadErrorAsync(response);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GroupTaskResponse>();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        LastError = null;
        var response = await DeleteAsync($"tasks/{id}");
        if (response.IsSuccessStatusCode)
            return true;

        LastError = await ReadErrorAsync(response);
        return false;
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            return error?.Message;
        }
        catch
        {
            return null;
        }
    }

    private sealed class ApiError
    {
        public string? Message { get; set; }
    }

    private sealed class CountResponse
    {
        public int Count { get; set; }
    }
}
