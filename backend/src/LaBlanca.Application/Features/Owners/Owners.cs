using FluentValidation;
using LaBlanca.Application.Abstractions.Messaging;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Features.Leads;
using LaBlanca.Application.Features.Properties;
using LaBlanca.Domain.Common;
using LaBlanca.Domain.Owners;
using LaBlanca.Domain.Properties;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Application.Features.Owners;

public sealed record OwnerPropertyDto(Guid Id, string Title, PropertyStatus Status);

public sealed record OwnerDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string? Document,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<OwnerPropertyDto> Properties);

public sealed record GetOwnersQuery(int Page = 1, int PageSize = GetOwnersQuery.MaxPageSize) : IQuery<PagedResult<OwnerDto>>
{
    public const int MaxPageSize = 500;
}

public sealed class GetOwnersQueryHandler(IAppDbContext db) : IRequestHandler<GetOwnersQuery, PagedResult<OwnerDto>>
{
    public async Task<PagedResult<OwnerDto>> Handle(GetOwnersQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, GetOwnersQuery.MaxPageSize);
        var page = Math.Max(request.Page, 1);
        var total = await db.Owners.CountAsync(cancellationToken);
        var owners = await db.Owners.AsNoTracking().OrderBy(o => o.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var ownerIds = owners.Select(o => o.Id).ToList();
        var properties = await db.Properties.AsNoTracking()
            .Where(p => p.OwnerId != null && ownerIds.Contains(p.OwnerId.Value))
            .Select(p => new
            {
                OwnerId = p.OwnerId!.Value,
                p.Id,
                p.Status,
                Title = db.PropertyTranslations.Where(t => t.PropertyId == p.Id && t.Locale == Locales.Default).Select(t => t.Title).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OwnerDto>(
            [.. owners.Select(o => new OwnerDto(
                o.Id, o.Name, o.Phone, o.Email, o.Document, o.Notes, o.CreatedAt, o.UpdatedAt,
                [.. properties.Where(p => p.OwnerId == o.Id).Select(p => new OwnerPropertyDto(p.Id, p.Title ?? string.Empty, p.Status))]))],
            total,
            page,
            pageSize);
    }
}

public sealed record SaveOwnerCommand(Guid? Id, string Name, string Phone, string? Email, string? Document, string? Notes) : ICommand<Guid>;

public sealed class SaveOwnerCommandValidator : AbstractValidator<SaveOwnerCommand>
{
    public SaveOwnerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Phone).ContactPhone();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Document).MaximumLength(40);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class SaveOwnerCommandHandler(IAppDbContext db) : IRequestHandler<SaveOwnerCommand, Guid>
{
    public async Task<Guid> Handle(SaveOwnerCommand request, CancellationToken cancellationToken)
    {
        if (request.Id is { } id)
        {
            var owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == id, cancellationToken) ?? throw new NotFoundException();
            owner.Update(request.Name, request.Phone, request.Email, request.Document, request.Notes);
            return owner.Id;
        }

        var created = Owner.Create(request.Name, request.Phone, request.Email, request.Document, request.Notes);
        db.Owners.Add(created);
        return created.Id;
    }
}

public sealed record DeleteOwnerCommand(Guid Id) : ICommand;

public sealed class DeleteOwnerCommandHandler(IAppDbContext db) : IRequestHandler<DeleteOwnerCommand>
{
    public async Task Handle(DeleteOwnerCommand request, CancellationToken cancellationToken)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken) ?? throw new NotFoundException();
        if (await db.Properties.AnyAsync(p => p.OwnerId == owner.Id && p.Status != PropertyStatus.Archived, cancellationToken))
        {
            throw new ConflictException(MessageKeys.OwnerHasActiveProperties);
        }

        db.Owners.Remove(owner);
    }
}
