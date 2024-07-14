namespace Client.Clients;

public interface IApiAppClient
{
    Task<string> AskToBot(string question);
}

public class ApiAppClient : IApiAppClient
{
    private readonly HttpClient _http;

    public ApiAppClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<string> AskToBot(string question)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "melonchart")
        {
            Content = JsonContent.Create(new { question })
        };
        
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        
        var res = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return res;
    }
}