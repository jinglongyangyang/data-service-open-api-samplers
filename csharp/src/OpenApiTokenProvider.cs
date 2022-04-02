using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using IdentityModel.Client;
using PlaywrightSharp;

namespace UiPath.DataService.Samples
{
    public class OpenApiTokenProvider
    {
        private readonly OpenApiCredentials _credentials;
        private readonly string _identityUri;

        public OpenApiTokenProvider(string dataServiceUrl, OpenApiCredentials credentials)
        {
            _credentials = credentials;
            var uri = new Uri(dataServiceUrl);
            _identityUri = $"{uri.Scheme}://{uri.Host}/identity_";
        }

        public async Task<OpenApiAccessToken> GetAccessTokenByRefreshTokenAsync()
        {
            var tokenRequest = new RefreshTokenRequest()
            {
                ClientId = _credentials.ClientId,
                ClientSecret = _credentials.ClientSecret,
                RefreshToken = _credentials.RefreshToken,
                Address = $"{_identityUri}/connect/token"
            };
            using var httpClient = new HttpClient();
            var response = await httpClient.RequestRefreshTokenAsync(tokenRequest);
            if (response.IsError)
            {
                if (response.Exception != null)
                {
                    throw new Exception("Fail to call Identity", response.Exception);
                }

                throw new Exception(response.Error);
            }
            return new OpenApiAccessToken()
            {
                AccessToken = response.AccessToken,
                TokenType = response.TokenType,
                Scope = response.Scope,
                ExpiresIn = response.ExpiresIn,
                RefreshToken = response.RefreshToken,
            };
        }

        public async Task<OpenApiAccessToken> GetAccessTokenByUserCredentialsAsync()
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

            if (response.IsError)
            {
                throw new Exception(response.ErrorDescription);
            }
            return new OpenApiAccessToken()
            {
                AccessToken = response.AccessToken,
                TokenType = response.TokenType,
                Scope = response.Scope,
                ExpiresIn = response.ExpiresIn,
                RefreshToken = response.RefreshToken,
            };
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
}