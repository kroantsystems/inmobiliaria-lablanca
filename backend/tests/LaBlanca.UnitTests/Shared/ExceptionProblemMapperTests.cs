using LaBlanca.Domain.Common;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LaBlanca.UnitTests.Shared;

public class ExceptionProblemMapperTests
{
    private readonly ExceptionProblemMapper _mapper = new(Options.Create(new ExceptionMappingOptions().Map<DomainException>(StatusCodes.Status400BadRequest)));

    [Fact]
    public void Validation_exception_maps_to_400_with_errors_by_field()
    {
        using var _ = new UiCultureScope("pt");
        var errors = new Dictionary<string, string[]> { ["email"] = ["E-mail inválido."] };

        var problem = _mapper.Map(new RequestValidationException(errors));

        problem.Status.Should().Be(400);
        problem.Detail.Should().Be("Um ou mais campos são inválidos.");
        problem.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Not_found_exception_maps_to_404()
    {
        using var _ = new UiCultureScope("es");

        var problem = _mapper.Map(new NotFoundException());

        problem.Status.Should().Be(404);
        problem.Detail.Should().Be("No se encontró el recurso solicitado.");
        problem.Errors.Should().BeNull();
    }

    [Theory]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(BusinessRuleException), 400)]
    [InlineData(typeof(UnauthorizedException), 401)]
    [InlineData(typeof(ForbiddenException), 403)]
    public void App_exceptions_map_to_their_status(Type type, int status)
    {
        var exception = (Exception)Activator.CreateInstance(type, MessageKeys.Unexpected, Array.Empty<object?>())!;

        _mapper.Map(exception).Status.Should().Be(status);
    }

    [Fact]
    public void Registered_domain_exception_uses_its_message_as_key()
    {
        using var _ = new UiCultureScope("en");

        var problem = _mapper.Map(new DomainException(MessageKeys.Forbidden));

        problem.Status.Should().Be(400);
        problem.Detail.Should().Be("You do not have permission to perform this action.");
    }

    [Fact]
    public void Bad_http_request_keeps_its_status()
    {
        var problem = _mapper.Map(new BadHttpRequestException("too large", StatusCodes.Status413PayloadTooLarge));

        problem.Status.Should().Be(413);
    }

    [Fact]
    public void Unknown_exception_maps_to_500_with_generic_message()
    {
        using var _ = new UiCultureScope("pt");

        var problem = _mapper.Map(new InvalidOperationException("connection string leaked here"));

        problem.Status.Should().Be(500);
        problem.Detail.Should().Be("Ocorreu um erro inesperado. Tente novamente mais tarde.");
        problem.Detail.Should().NotContain("connection string");
    }
}
