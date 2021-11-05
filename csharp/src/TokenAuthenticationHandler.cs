using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace UiPath.DataService.Samples
{
    public class TokenAuthenticationHandler : DelegatingHandler
    {
        private readonly OpenApiTokenProvider _tokenProvider;
        private readonly string _accessToken;

        public TokenAuthenticationHandler(string accessToken) : base(new HttpClientHandler())
        {
            _accessToken = accessToken;
        }

        public TokenAuthenticationHandler(string dataServiceUrl, OpenApiCredentials credentials) : base(new HttpClientHandler())
        {
            _tokenProvider = new OpenApiTokenProvider(dataServiceUrl, credentials);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authorization = request.Headers.Authorization;
            if (request.Headers.Authorization?.Scheme != "Bearer")
            {
                if (string.IsNullOrEmpty(_accessToken) && _tokenProvider == null)
                {
                    throw new ArgumentException("");
                }

                var token = !string.IsNullOrEmpty(_accessToken) ? _accessToken : (await _tokenProvider.GetAccessTokenAsync());
                request.SetBearerToken(token); ;
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}