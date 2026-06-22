using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class PollService : ApiService
{
    public PollService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<MessageResponse?> CreatePollAsync(CreatePollRequest request)
    {
        return await PostAndGetJsonAsync<MessageResponse>("polls", request);
    }

    public async Task<bool> VoteAsync(int pollId, List<int> optionIds)
    {
        try
        {
            var response = await PostAsync($"polls/{pollId}/vote", new PollVoteRequest { OptionIds = optionIds });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnvoteAsync(int pollId)
    {
        try
        {
            var response = await DeleteAsync($"polls/{pollId}/vote");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PollResponse?> GetPollAsync(int pollId)
    {
        return await GetFromJsonAsync<PollResponse>($"polls/{pollId}");
    }
}
