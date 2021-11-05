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

            string dataServiceUrl = "https://alpha.uipath.com/entity/hr/dataservice_/api";

            // If have access token already, could copy and past the token to BuildTokenAuthenticationHandler and uncomment it
            // var tokenAuthenticationHandler = BuildTokenAuthenticationHandler(); 
            var tokenAuthenticationHandler = BuildTokenAuthenticationHandler(dataServiceUrl);
            var client = new DataServiceOpenApiClient(new HttpClient(tokenAuthenticationHandler)) { BaseUrl = dataServiceUrl };

            var leadActor = await GetOrCreateActor(client, "Andy", "Lau");
            Console.WriteLine(leadActor.Id);

            var director = await GetOrCreateActor(client, "Andy", "Lau");
            Console.WriteLine(director.Id);

            var movie = await GetOrCreateMovie(client, "", director, leadActor);
            Console.WriteLine(movie.Id);
        }

        private static TokenAuthenticationHandler BuildTokenAuthenticationHandler()
        {
            return new TokenAuthenticationHandler("token");
        }

        private static TokenAuthenticationHandler BuildTokenAuthenticationHandler(string dataServiceUrl)
        {
            // Confidential external application registered in UiPath Automation Cloud
            var credentials = new OpenApiCredentials()
            {
                ClientId = "",
                ClientSecret = "",
                Email = "",
                Password = "",
                RedirectUri = "",
                Scope = "", //Space separated scopes like "DataService.Schema.Read DataService.Data.Read DataService.Data.Write";
            };
            return new TokenAuthenticationHandler(dataServiceUrl, credentials);
        }

        private static async Task<Actor> GetOrCreateActor(DataServiceOpenApiClient client, string first, string last)
        {
            ICollection<QueryFilter> filters = new List<QueryFilter>() { QueryFilter(nameof(Actor.First), first), QueryFilter(nameof(Actor.Last), last) };
            var request = new QueryRequest() { FilterGroup = new QueryFilterGroup() { QueryFilters = filters } };
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
            return new QueryFilter() { FieldName = field, Value = value };
        }
    }
}
