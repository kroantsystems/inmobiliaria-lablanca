namespace LaBlanca.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}

/// <summary>Entidade isolada por imobiliária. O <see cref="TenantId"/> é preenchido pela persistência a partir do contexto.</summary>
public abstract class TenantEntity : Entity
{
    public Guid TenantId { get; protected set; }
}

/// <summary>Datas preenchidas pela persistência ao inserir e alterar.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? UpdatedAt { get; }
}
