using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Behaviors;
using LaBlanca.Shared.Errors;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace LaBlanca.UnitTests.Application.Behaviors;

public class BehaviorTests
{
    public sealed record SampleCommand(string Email, string Password) : ICommand<string>;

    public sealed record SampleQuery(int Id) : IQuery<string>;

    private sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    [Fact]
    public async Task Validation_behavior_throws_and_skips_handler_for_invalid_command()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>([new SampleCommandValidator()]);
        var handlerCalled = false;

        var act = () => behavior.Handle(new SampleCommand("not-an-email", ""), _ =>
        {
            handlerCalled = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<RequestValidationException>()).Which;
        exception.Errors.Keys.Should().BeEquivalentTo("email", "password");
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Validation_behavior_calls_handler_for_valid_command()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>([new SampleCommandValidator()]);

        var result = await behavior.Handle(new SampleCommand("a@b.com", "x"), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Validation_behavior_uses_camel_case_paths_for_nested_properties()
    {
        var validator = new InlineValidator<SampleCommand>();
        validator.RuleFor(x => x.Email).Must(_ => false).OverridePropertyName("Translations[0].Title");
        var behavior = new ValidationBehavior<SampleCommand, string>([validator]);

        var act = () => behavior.Handle(new SampleCommand("a@b.com", "x"), _ => Task.FromResult("ok"), CancellationToken.None);

        (await act.Should().ThrowAsync<RequestValidationException>()).Which.Errors.Keys.Should().Equal("translations[0].title");
    }

    [Fact]
    public async Task Transaction_behavior_saves_and_commits_command()
    {
        var (uow, tx) = UnitOfWorkMocks();
        var behavior = new TransactionBehavior<SampleCommand, string>(uow.Object);

        await behavior.Handle(new SampleCommand("a@b.com", "x"), _ => Task.FromResult("ok"), CancellationToken.None);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Transaction_behavior_rolls_back_when_handler_fails()
    {
        var (uow, tx) = UnitOfWorkMocks();
        var behavior = new TransactionBehavior<SampleCommand, string>(uow.Object);

        var act = () => behavior.Handle(new SampleCommand("a@b.com", "x"), _ => throw new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Transaction_behavior_does_not_open_transaction_for_query()
    {
        var (uow, _) = UnitOfWorkMocks();
        var behavior = new TransactionBehavior<SampleQuery, string>(uow.Object);

        await behavior.Handle(new SampleQuery(1), _ => Task.FromResult("ok"), CancellationToken.None);

        uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logging_behavior_logs_request_name_without_payload()
    {
        var logger = new CapturingLogger<LoggingBehavior<SampleCommand, string>>();
        var behavior = new LoggingBehavior<SampleCommand, string>(logger);

        await behavior.Handle(new SampleCommand("a@b.com", "SuperSecret123"), _ => Task.FromResult("ok"), CancellationToken.None);

        logger.Messages.Should().Contain(m => m.Contains(nameof(SampleCommand)));
        logger.Messages.Should().NotContain(m => m.Contains("SuperSecret123") || m.Contains("a@b.com"));
    }

    private static (Mock<IUnitOfWork> Uow, Mock<IUnitOfWorkTransaction> Tx) UnitOfWorkMocks()
    {
        var tx = new Mock<IUnitOfWorkTransaction>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tx.Object);
        return (uow, tx);
    }
}

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Messages.Add(formatter(state, exception));
}
