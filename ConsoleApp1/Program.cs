using System.Net.Http;

var client = new HttpClient();
var response = await client.GetAsync("http://localhost:15091");
var body = await response.Content.ReadAsStringAsync();

Console.WriteLine($"{(int)response.StatusCode}: {response.StatusCode}");
Console.WriteLine(body);

Console.ReadLine();