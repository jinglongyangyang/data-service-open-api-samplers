using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace UiPath.DataService.Samples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var dataServiceUrl = "https://cloud.uipath.com/vz/vz/dataservice_/api";

            var accessToken = await GetTokenByRefreshToken(dataServiceUrl, "");
            //var accessToken = await GetTokenByUserCredentials(dataServiceUrl);
            Console.WriteLine($"Access token [{accessToken.AccessToken}]");
            Console.WriteLine($"Refresh token [{accessToken.RefreshToken}]");
            Console.WriteLine($"Scope[{accessToken.Scope}]");
            Console.WriteLine($"ExpiresIn [{accessToken.ExpiresIn}] seconds");


            var client = new DataServiceOpenApiClient(new HttpClient(new TokenAuthenticationHandler(accessToken.AccessToken))) { BaseUrl = dataServiceUrl };

            var leadActor = await GetOrCreateActor(client, "Andy", "Lau");
            Console.WriteLine(leadActor.Id);

            var director = await GetOrCreateActor(client, "Andy", "Lau");
            Console.WriteLine(director.Id);

            var movie = await GetOrCreateMovie(client, "Test", director, leadActor);
            Console.WriteLine(movie.Id);
        }

        private static async Task<OpenApiAccessToken> GetTokenByRefreshToken(string dataServiceUrl)
        {
            // This sample only supports login with email
            var tokenProvider = new OpenApiTokenProvider(dataServiceUrl, new OpenApiCredentials()
            {
                ClientId = "", // external app id
                ClientSecret = "", // external app secret
                RefreshToken = "", // refresh token
            });
            return await tokenProvider.GetAccessTokenByRefreshTokenAsync();
        }

        private static async Task<OpenApiAccessToken> GetTokenByUserCredentials(string dataServiceUrl)
        {
            // This sample only supports login with email
            var tokenProvider = new OpenApiTokenProvider(dataServiceUrl, new OpenApiCredentials()
            {
                ClientId = "", // external app id
                ClientSecret = "", // external app secret
                Email = "", // automation cloud user
                Password = "", // automation cloud password
                RedirectUri = "", // Redirect uri
                Scope = "DataService.Schema.Read DataService.Data.Read DataService.Data.Write offline_access", //Space separated scopes, offline_access is for getting refresh token
            });
            return await tokenProvider.GetAccessTokenByUserCredentialsAsync();
        }

        private static async Task<Actor> GetOrCreateActor(DataServiceOpenApiClient client, string first, string last)
        {
            ICollection<QueryFilter> filters = new List<QueryFilter>() { QueryFilter(nameof(Actor.First), first), QueryFilter(nameof(Actor.Last), last) };
            var request = new QueryRequest() { FilterGroup = new QueryFilterGroup() { QueryFilters = filters, LogicalOperator = 1 } };
            var response = await client.Query_ActorAsync(2, request);
            var actors = response.Value;
            if (actors == null || actors.Count == 0)
            {
                var actor = new Actor()
                {
                    First = first,
                    Last = last
                };
                return await client.Add_ActorAsync(2, actor);
            }
            return actors.First();
        }

        private static async Task<Movie> GetOrCreateMovie(DataServiceOpenApiClient client, string name, Actor director, Actor leadActor)
        {
            ICollection<QueryFilter> filters = new List<QueryFilter>() { QueryFilter(nameof(Movie.Name), name) };
            var request = new QueryRequest() { FilterGroup = new QueryFilterGroup() { QueryFilters = filters } };
            var response = await client.Query_MovieAsync(2, request);
            var movies = response.Value;
            if (movies == null || movies.Count == 0)
            {
                var actor = new Movie()
                {
                    Name = name,
                    Director = director,
                    LeadActor = leadActor,
                };
                return await client.Add_MovieAsync(2, actor);
            }
            return movies.First();
        }

        private static QueryFilter QueryFilter(string field, string value)
        {
            return new QueryFilter() { FieldName = field, Value = value, Operator = "=" };
        }
    }
}
