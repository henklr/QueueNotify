using System;
using System.Net.Http;
using System.Threading.Tasks;

public class PushoverService
{
    private const string PushoverApiUrl = "https://api.pushover.net/1/messages.json";
    private const string UserKey = "your_userkey";
    private const string AppToken = "your_apitoken";

    public async Task SendPushoverNotificationAsync(string message)
    {
        using (var httpClient = new HttpClient())
        {
            var parameters = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user", UserKey),
                new KeyValuePair<string, string>("token", AppToken),
                new KeyValuePair<string, string>("message", message)
            });

            var response = await httpClient.PostAsync(PushoverApiUrl, parameters);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Pushover response: {response.StatusCode}, {responseContent}");
        }
    }
}
