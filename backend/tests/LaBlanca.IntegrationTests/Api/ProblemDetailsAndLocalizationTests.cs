using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LaBlanca.IntegrationTests.Infrastructure;

namespace LaBlanca.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class ProblemDetailsAndLocalizationTests(ApiFactory factory)
{
    [Fact]
    public async Task Not_found_is_returned_as_problem_details_in_default_language()
    {
        var response = await factory.CreateClient().GetAsync("/api/public/_test/not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await ReadAsync(response);
        problem.GetProperty("title").GetString().Should().Be("Não encontrado");
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.GetProperty("detail").GetString().Should().Be("O recurso solicitado não foi encontrado.");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("es-PY", "No encontrado")]
    [InlineData("en-US,en;q=0.9", "Not found")]
    [InlineData("gn", "No encontrado")]
    [InlineData("fr", "Não encontrado")]
    public async Task Problem_title_follows_accept_language(string acceptLanguage, string expectedTitle)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/_test/not-found");
        request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);

        var problem = await ReadAsync(await factory.CreateClient().SendAsync(request));

        problem.GetProperty("title").GetString().Should().Be(expectedTitle);
    }

    [Fact]
    public async Task Validation_errors_are_grouped_by_field_and_localized()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/_test/validate")
        {
            Content = JsonContent.Create(new { email = "not-an-email", name = "" }),
        };
        request.Headers.Add("Accept-Language", "es-PY");

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadAsync(response);
        problem.GetProperty("title").GetString().Should().Be("Solicitud inválida");
        problem.GetProperty("detail").GetString().Should().Be("Uno o más campos no son válidos.");
        var errors = problem.GetProperty("errors");
        errors.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("email", "name");
        errors.GetProperty("name")[0].GetString().Should().Contain("vac");
    }

    [Fact]
    public async Task Valid_command_reaches_the_handler()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/public/_test/validate", new { email = "a@b.com", name = "Ana" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Ana");
    }

    [Fact]
    public async Task Domain_exception_becomes_400_with_translated_message()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/_test/domain");
        request.Headers.Add("Accept-Language", "en");

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadAsync(response)).GetProperty("detail").GetString().Should().Be("You do not have permission to perform this action.");
    }

    [Fact]
    public async Task Unexpected_error_hides_internal_details_outside_development()
    {
        var response = await factory.CreateClient().GetAsync("/api/public/_test/boom");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("hunter2").And.NotContain("secret-db").And.NotContain("InvalidOperationException");
        var problem = JsonDocument.Parse(body).RootElement;
        problem.GetProperty("detail").GetString().Should().Be("Ocorreu um erro inesperado. Tente novamente mais tarde.");
        problem.TryGetProperty("exception", out _).Should().BeFalse();
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
