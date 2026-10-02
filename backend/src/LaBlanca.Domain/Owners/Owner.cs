using LaBlanca.Domain.Common;

namespace LaBlanca.Domain.Owners;

public sealed class Owner : TenantEntity, IAuditable
{
    private Owner()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Document { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Owner Create(string name, string phone, string? email, string? document, string? notes)
    {
        var owner = new Owner();
        owner.Update(name, phone, email, document, notes);
        return owner;
    }

    public void Update(string name, string phone, string? email, string? document, string? notes)
    {
        Name = name.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
