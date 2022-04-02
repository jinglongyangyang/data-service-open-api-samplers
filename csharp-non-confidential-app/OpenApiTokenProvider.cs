using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using IdentityModel;
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

        public async Task<OpenApiAccessToken> GetAccessTokenByUserCredentialsAsync()
        {
            var pkce = CreatePkceData();
            var code = await LoginIntoAutomationCloud(pkce.CodeChallenge);
            var tokenRequest = new AuthorizationCodeTokenRequest()
            {
                ClientId = _credentials.ClientId,
                Code = code,
                CodeVerifier = pkce.CodeVerifier,
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
            };
        }

        private async Task<string> LoginIntoAutomationCloud(string codeChallenge)
        {
            // Authorize the user with the 3rd party client via headless browser
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(false);
            var page = await browser.NewPageAsync();
            await page.GoToAsync($"{_identityUri}/connect/authorize?client_id={_credentials.ClientId}&redirect_uri={Uri.EscapeDataString(_credentials.RedirectUri)}&scope={Uri.EscapeDataString(_credentials.Scope)}&response_type=code&code_challenge={codeChallenge}&code_challenge_method=S256");
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

        private static Pkce CreatePkceData()
        {
            var pkce = new Pkce
            {
                CodeVerifier = CryptoRandom.CreateUniqueId()
            };

            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pkce.CodeVerifier));
            pkce.CodeChallenge = Base64Url.Encode(challengeBytes);

            return pkce;
        }
    }

    internal class Pkce
    {
        public string CodeVerifier { get; set; }

        public string CodeChallenge { get; set; }
    }
}