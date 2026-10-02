using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Abstractions.Revalidation;
using LaBlanca.Domain.Properties;
using LaBlanca.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Properties;

public enum PropertyAction
{
    Publish,
    Unpublish,
    Feature,
    Unfeature,
}

public sealed record ChangePropertyPublicationCommand(Guid Id, PropertyAction Action) : ICommand;

public sealed record ChangePropertyStatusCommand(Guid Id, PropertyStatus Status) : ICommand;

internal static class PropertyLoading
{
    public static async Task<Property> LoadForLifecycleAsync(this IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Properties.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == id, cancellationToken) ?? throw new NotFoundException();
}

public sealed class ChangePropertyPublicationCommandHandler(IAppDbContext db, IRevalidationNotifier revalidation, TimeProvider clock)
    : IRequestHandler<ChangePropertyPublicationCommand>
{
    public async Task Handle(ChangePropertyPublicationCommand request, CancellationToken cancellationToken)
    {
        var property = await db.LoadForLifecycleAsync(request.Id, cancellationToken);
        switch (request.Action)
        {
            case PropertyAction.Publish:
                property.Publish(clock.GetUtcNow());
                break;
            case PropertyAction.Unpublish:
                property.Unpublish();
                break;
            case PropertyAction.Feature:
                property.SetFeatured(true);
                break;
            case PropertyAction.Unfeature:
                property.SetFeatured(false);
                break;
        }

        revalidation.Request(CacheTags.ForProperty(property.Id));
    }
}

public sealed class ChangePropertyStatusCommandHandler(IAppDbContext db, IRevalidationNotifier revalidation, TimeProvider clock)
    : IRequestHandler<ChangePropertyStatusCommand>
{
    public async Task Handle(ChangePropertyStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            throw new NotFoundException();
        }

        var property = await db.LoadForLifecycleAsync(request.Id, cancellationToken);
        property.ChangeStatus(request.Status, clock.GetUtcNow());
        revalidation.Request(CacheTags.ForProperty(property.Id));
    }
}
