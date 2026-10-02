using FluentValidation;
using LaBlanca.Api.Configuration;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Authorization;
using LaBlanca.Domain.Common;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LaBlanca.IntegrationTests.Infrastructure;

// Endpoints só para testes da plataforma; rotas com "_test" são ignoradas no teste de cobertura de autorização.
[ApiController]
[AllowAnonymous]
[Route("api/public/_test")]
public sealed class PublicTestController(ISender sender) : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() => throw new NotFoundException();

    [HttpGet("domain")]
    public IActionResult ThrowDomain() => throw new DomainException(MessageKeys.Forbidden);

    [HttpGet("boom")]
    public IActionResult ThrowUnexpected() => throw new InvalidOperationException("Host=secret-db;Password=hunter2");

    [HttpPost("validate")]
    public async Task<IActionResult> Validate(TestCommand command) => Ok(await sender.Send(command));

    [HttpPost("form")]
    [EnableRateLimiting(RateLimitPolicies.PublicForms)]
    public IActionResult Form() => Accepted();
}

[ApiController]
[Route("api/admin/_test")]
public sealed class AdminTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();

    [HttpPost]
    [Authorize(Policy = Permissions.PropertiesWrite)]
    public IActionResult Create() => StatusCode(StatusCodes.Status201Created);
}

public sealed record TestCommand(string Email, string Name) : ICommand<string>;

public sealed class TestCommandValidator : AbstractValidator<TestCommand>
{
    public TestCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class TestCommandHandler : IRequestHandler<TestCommand, string>
{
    public Task<string> Handle(TestCommand request, CancellationToken cancellationToken) => Task.FromResult(request.Name);
}
