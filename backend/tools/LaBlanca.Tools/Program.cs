using LaBlanca.Application;
using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Application.Features.Auth;
using LaBlanca.Infrastructure;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string Usage = """
    LaBlanca.Tools — administração de usuários (não existe cadastro pela API)

      create-admin   --tenant la-blanca --name "Nome" --email admin@exemplo.com [--password-stdin]
      reset-password --tenant la-blanca --email admin@exemplo.com [--password-stdin]
      hash-password  [--password-stdin]

    Conexão: variável ConnectionStrings__Default ou --connection "<connection string>".
    Sem --password-stdin a senha é pedida no terminal, sem eco.
    """;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine(Usage);
    return 0;
}

var command = args[0];
var builder = Host.CreateApplicationBuilder(args[1..]);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Configuration["BackgroundJobs:Enabled"] = "false";
if (builder.Configuration["connection"] is { Length: > 0 } connection)
{
    builder.Configuration["ConnectionStrings:Default"] = connection;
}

var options = builder.Configuration;
var readFromStdin = args.Contains("--password-stdin");

try
{
    if (command == "hash-password")
    {
        Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(ReadPassword(readFromStdin, confirm: true), 12));
        return 0;
    }

    builder.Services.AddSingleton<ITenantContext, NoTenantContext>();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var sender = scope.ServiceProvider.GetRequiredService<ISender>();

    switch (command)
    {
        case "create-admin":
            var id = await sender.Send(new CreateAdminCommand(
                Required(options, "tenant"),
                Required(options, "name"),
                Required(options, "email"),
                ReadPassword(readFromStdin, confirm: true)));
            Console.WriteLine($"Administrador criado: {id}");
            return 0;

        case "reset-password":
            await sender.Send(new ResetPasswordCommand(
                Required(options, "tenant"),
                Required(options, "email"),
                ReadPassword(readFromStdin, confirm: true)));
            Console.WriteLine("Senha redefinida e sessões encerradas.");
            return 0;

        default:
            Console.Error.WriteLine($"Comando desconhecido: {command}");
            Console.WriteLine(Usage);
            return 1;
    }
}
catch (RequestValidationException validation)
{
    foreach (var (field, messages) in validation.Errors)
    {
        Console.Error.WriteLine($"{field}: {string.Join(" ", messages)}");
    }

    return 1;
}
catch (AppException app)
{
    Console.Error.WriteLine(AppMessages.Get(app.MessageKey, app.MessageArgs));
    return 1;
}
catch (ArgumentException missing)
{
    Console.Error.WriteLine(missing.Message);
    return 1;
}

static string Required(IConfiguration options, string name) =>
    options[name] is { Length: > 0 } value ? value : throw new ArgumentException($"Informe --{name}.");

static string ReadPassword(bool fromStdin, bool confirm)
{
    if (fromStdin)
    {
        return Console.In.ReadLine() ?? string.Empty;
    }

    var password = Prompt("Senha: ");
    if (confirm && Prompt("Confirme a senha: ") != password)
    {
        throw new ArgumentException("As senhas não coincidem.");
    }

    return password;
}

static string Prompt(string label)
{
    Console.Write(label);
    var buffer = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return buffer.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (buffer.Length > 0)
            {
                buffer.Length--;
            }
        }
        else if (!char.IsControl(key.KeyChar))
        {
            buffer.Append(key.KeyChar);
        }
    }
}

internal sealed class NoTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Empty;
}
