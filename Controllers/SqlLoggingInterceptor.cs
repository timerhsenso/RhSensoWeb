using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class SqlLoggingInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SqlLoggingInterceptor> _log;
    private readonly IHttpContextAccessor _http;
    private readonly IHostEnvironment _env;

    public SqlLoggingInterceptor(
        ILogger<SqlLoggingInterceptor> log,
        IHttpContextAccessor http,
        IHostEnvironment env)
    {
        _log = log; _http = http; _env = env;
    }

    private string Who()
    {
        var ctx = _http.HttpContext;
        var user = ctx?.User?.Identity?.Name ?? "anon";
        var path = ctx?.Request?.Path.Value ?? "";
        var rid = ctx?.TraceIdentifier ?? "";
        return $"user={user} path={path} rid={rid}";
    }

    private static string Params(DbCommand cmd) =>
        string.Join(", ", cmd.Parameters.Cast<DbParameter>()
            .Select(p => $"{p.ParameterName}={(p.Value is null || p.Value is DBNull ? "NULL" : Trunc(p.Value.ToString(), 200))}"));

    private static string Trunc(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s!.Length <= max ? s : s[..max] + "…");

    // ===== SELECTs - antes de executar
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (_env.IsDevelopment())
            _log.LogInformation("SQL> {who} [{id}] {sql} || {parms}",
                Who(), eventData.CommandId, command.CommandText, Params(command));
        else
            _log.LogInformation("SQL> {who} [{id}] {sql}",
                Who(), eventData.CommandId, command.CommandText);

        return base.ReaderExecuting(command, eventData, result);
    }

    // ===== SELECTs - depois de executar (RETORNA DbDataReader)
    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result)
    {
        _log.LogInformation("SQL✓ {who} [{id}] {ms} ms",
            Who(), eventData.CommandId, eventData.Duration.TotalMilliseconds);

        return base.ReaderExecuted(command, eventData, result);
    }

    // ===== INSERT/UPDATE/DELETE - antes
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result)
    {
        if (_env.IsDevelopment())
            _log.LogInformation("SQL> {who} [{id}] {sql} || {parms}",
                Who(), eventData.CommandId, command.CommandText, Params(command));
        else
            _log.LogInformation("SQL> {who} [{id}] {sql}",
                Who(), eventData.CommandId, command.CommandText);

        return base.NonQueryExecuting(command, eventData, result);
    }

    // ===== INSERT/UPDATE/DELETE - depois (RETORNA int)
    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData,
        int result)
    {
        _log.LogInformation("SQL✓ {who} [{id}] {ms} ms; affected={rows}",
            Who(), eventData.CommandId, eventData.Duration.TotalMilliseconds, result);

        return base.NonQueryExecuted(command, eventData, result);
    }

    // ===== SELECT escalar - antes (ex.: COUNT(*))
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<object> result)
    {
        if (_env.IsDevelopment())
            _log.LogInformation("SQL> {who} [{id}] {sql} || {parms}",
                Who(), eventData.CommandId, command.CommandText, Params(command));
        else
            _log.LogInformation("SQL> {who} [{id}] {sql}",
                Who(), eventData.CommandId, command.CommandText);

        return base.ScalarExecuting(command, eventData, result);
    }

    // ===== SELECT escalar - depois (RETORNA object?)
    public override object? ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData,
        object? result)
    {
        _log.LogInformation("SQL✓ {who} [{id}] {ms} ms; scalar={scalar}",
            Who(), eventData.CommandId, eventData.Duration.TotalMilliseconds, result);

        return base.ScalarExecuted(command, eventData, result);
    }

    // ===== Erros
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _log.LogError(eventData.Exception, "SQL✗ {who} [{id}] {sql}",
            Who(), eventData.CommandId, command.CommandText);
        base.CommandFailed(command, eventData);
    }
}
