using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using RevueCrafters.Models;
using System.Net;
using System.Text.Json;

namespace RevueCrafters
{



    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string RevueId;
        private const string BaseUrl = "https://d2925tksfvgq8c.cloudfront.net";

        [OneTimeSetUp]

        public void Setup()
        {
            string token = GetJwtToken("Rumka@example.com", "Rumka123");

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var loginClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { email, password });

            var response = loginClient.Execute(request);
           
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return jsonResponse.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]
        public void CreateRevue_ShouldReturnOk()
        {
            var revue = new
            {
                title = "New Revue",
                url = "",
                description = "Full Revue"
            };

            var request = new RestRequest("/api/Revue/Create", Method.Post);
            request.AddJsonBody(revue);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var apiResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(apiResponse, Is.Not.Null, "Response deserialization failed");
            Assert.That(apiResponse.Msg, Is.EqualTo("Successfully created!"));

            var getRequest = new RestRequest("/api/Revue/All", Method.Get);
            var getResponse = client.Execute(getRequest);
            var jsonArray = JsonSerializer.Deserialize<JsonElement>(getResponse.Content);
            int length = jsonArray.GetArrayLength();
            var lastRevue = jsonArray[length - 1]; ;
            RevueId = lastRevue.GetProperty("id").GetString();

            Assert.That(RevueId, Is.Not.Null.And.Not.Empty, "RevueId was not extracted correctly");
        }

        [Test, Order(2)]
        public void GetAllRevues_ShoulReturnOk()
        {
            var request = new RestRequest("/api/Revue/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty, "Response content is empty");


            var jsonArray = JsonSerializer.Deserialize<JsonElement>(response.Content);

            Assert.That(jsonArray.ValueKind, Is.EqualTo(JsonValueKind.Array), "Response is not a JSON array");
            Assert.That(jsonArray.GetArrayLength(), Is.GreaterThan(0), "Response array is empty");

        }


        [Test, Order(3)]
        public void EditRevue_ShouldReturnOk()
        {
            var changes = new
            {
                title = "Edited Revue",
                url = "",
                description = "Edited description"

            };
            var request = new RestRequest($"/api/Revue/Edit/", Method.Put);
            request.AddQueryParameter("revueId", RevueId);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(jsonResponse.GetProperty("msg").GetString(), Is.EqualTo("Edited successfully"), "Expected success message");
        }


        [Test, Order(4)]
        public void DeleteRevue_ShoulReturnOk()
        {
            var request = new RestRequest($"/api/Revue/Delete/", Method.Delete);
            request.AddQueryParameter("revueId", RevueId);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(jsonResponse.GetProperty("msg").GetString(), Is.EqualTo("The revue is deleted!"), "Expected success message");
        }


        [Test, Order(5)]
        public void CreateRevueWithoutTheRequiredFields_ShouldReturnBadRequest()
        {
            var revue = new
            {

            };
            var request = new RestRequest("/api/Revue/Create", Method.Post);
            request.AddJsonBody(revue);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");

        }

        [Test, Order(6)]
        public void EditNonExistingRevue_ShouldReturnBadRequest()
        {
            var fakeId = 678;
            var changes = new
            {
                title = "New Edited Revue",
                description = "New Updated description",
                url = ""
            };
            var request = new RestRequest($"/api/Revue/Edit", Method.Put);
            request.AddQueryParameter("revueId", fakeId);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(jsonResponse.GetProperty("msg").GetString(), Is.EqualTo("There is no such revue!"), "Expected error message for non-existing revue");


        }

        [Test, Order(7)]
        public void DeleteNonExistingRevue_ShouldReturnBadRequest()
        {
            var fakeId = 789;
            var request = new RestRequest($"/api/Revue/Delete/", Method.Delete);
            request.AddQueryParameter("revueId", fakeId);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(jsonResponse.GetProperty("msg").GetString(), Is.EqualTo("There is no such revue!"), "Expected error message for non-existing revue");
        }



        [OneTimeTearDown]
        public void CleanUp()
        {
            client?.Dispose();
        }

    }
}