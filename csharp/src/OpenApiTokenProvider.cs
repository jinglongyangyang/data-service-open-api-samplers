using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using IdentityModel.Client;
using PlaywrightSharp;

namespace UiPath.DataService.Samples
{
    public sealed class OpenApiTokenProvider
    {
        private readonly OpenApiCredentials _credentials;
        private readonly string _identityUri;
        private string _token;

        public OpenApiTokenProvider(string dataServiceUrl, OpenApiCredentials credentials)
        {
            _credentials = credentials;
            var uri = new Uri(dataServiceUrl);
            _identityUri = $"{uri.Scheme}://{uri.Host}/identity_";
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_token))
            {
                _token = await GetNewAccessTokenAsync();
            }
            return _token;
        }

        private async Task<string> GetNewAccessTokenAsync()
        {
            var code = await LoginIntoAutomationCloud();
            var tokenRequest = new AuthorizationCodeTokenRequest()
            {
                ClientId = _credentials.ClientId,
                ClientSecret = _credentials.ClientSecret,
                Code = code,
                RedirectUri = _credentials.RedirectUri,
                Address = $"{_identityUri}/connect/token"
            };
            using var httpClient = new HttpClient();
            var response = await httpClient.RequestAuthorizationCodeTokenAsync(tokenRequest);
            if (response.IsError)
            {
                if (response.Exception != null)
                {
                    throw new Exception("Fail to call Identity", response.Exception);
                }

                throw new Exception(response.Error);
            }
            return response.AccessToken;
        }

        private async Task<string> LoginIntoAutomationCloud()
        {
            // Authorize the user with the 3rd party client via headless browser
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(false);
            var page = await browser.NewPageAsync();
            await page.GoToAsync($"{_identityUri}/connect/authorize?client_id={_credentials.ClientId}&redirect_uri={Uri.EscapeDataString(_credentials.RedirectUri)}&scope={Uri.EscapeDataString(_credentials.Scope)}&response_type=code");
            await page.ClickAsync("text=Continue with Email");
            await page.FillAsync("[name=email]", _credentials.Email);
            await page.FillAsync("[name=password]", _credentials.Password);
            await page.ClickAsync("button >> text=Sign in");
            await page.WaitForLoadStateAsync(LifecycleEvent.Networkidle);

            // Gets authorization code
            var code = HttpUtility.ParseQueryString(new Uri(page.Url).Query).Get("code");
            await page.CloseAsync();
            return code;
        }
    }

    public class OpenApiCredentials
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Scope { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string RedirectUri { get; set; }
    }

}