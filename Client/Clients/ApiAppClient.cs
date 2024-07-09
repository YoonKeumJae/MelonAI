namespace Client.Clients;

public interface IApiAppClient
{
    Task<string> AskToBot(string question);
}

public class ApiAppClient(HttpClient http) : IApiAppClient
{
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));

    public async Task<string> AskToBot(string question)
    {
        //TODO: Implement the call to the API
        using var response = await _http.PostAsJsonAsync(
            "melonchart",
            new { question }).ConfigureAwait(false);

        var res = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return res;
    }
}