using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace LaBlanca.Infrastructure.Persistence;

/// <summary>
/// Liga Row Level Security (sem políticas) em toda tabela criada por migração. Assim a Data API do Supabase,
/// que usa os papéis <c>anon</c>/<c>authenticated</c>, não lê nada; a API conecta como dona das tabelas e não é afetada.
/// O SQL aparece ao aplicar migrações (update, script e bundle), não nos arquivos .cs gerados.
/// </summary>
#pragma warning disable EF1001 // INpgsqlSingletonOptions é a dependência exigida pelo gerador do Npgsql.
internal sealed class RlsMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, INpgsqlSingletonOptions npgsqlOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlOptions)
#pragma warning restore EF1001
{
    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        base.Generate(operation, model, builder, terminate);

        if (!terminate)
        {
            return;
        }

        var helper = Dependencies.SqlGenerationHelper;
        builder
            .Append("ALTER TABLE ")
            .Append(helper.DelimitIdentifier(operation.Name, operation.Schema))
            .Append(" ENABLE ROW LEVEL SECURITY")
            .AppendLine(helper.StatementTerminator);
        EndStatement(builder);
    }
}
