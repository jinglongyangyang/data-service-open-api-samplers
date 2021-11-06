using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace UiPath.DataService.Samples
{
    public class TokenAuthenticationHandler : DelegatingHandler
    {
        private readonly string _accessToken;

        public TokenAuthenticationHandler(string accessToken) : base(new HttpClientHandler())
        {
            _accessToken = accessToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization?.Scheme != "Bearer")
            {
                request.SetBearerToken(_accessToken); ;
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}