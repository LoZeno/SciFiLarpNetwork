using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi;
using SciFiLarpNetwork.Backend.Api;
using SciFiLarpNetwork.Backend.Api.ErrorResponses;
using SciFiLarpNetwork.Backend.Application.Administration;
using SciFiLarpNetwork.Backend.Application.EventSourcing;
using SciFiLarpNetwork.Backend.Infrastructure;
using SciFiLarpNetwork.Backend.Localization.Text;
using SciFiLarpNetwork.Common.Siren;

var operatorCommand = args.SingleOrDefault(argument => argument is "--bootstrap-admin" or "--recover-admin");
var hostArguments = operatorCommand is null
    ? args
    : args.Where(argument => !string.Equals(argument, operatorCommand, StringComparison.Ordinal)).ToArray();
var builder = WebApplication.CreateBuilder(hostArguments);

builder.Services
    .AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var responseFactory = context.HttpContext.RequestServices.GetRequiredService<IApiErrorResponseFactory>();
        var response = responseFactory.CreateFromModelState(context.HttpContext, context.ModelState);

        return new ObjectResult(response)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { SirenMediaTypes.Json }
        };
    };
});

builder.Services.Configure<MvcOptions>(options =>
{
    var jsonOutputFormatter = options.OutputFormatters
        .OfType<SystemTextJsonOutputFormatter>()
        .Single();
    jsonOutputFormatter.SupportedMediaTypes.Add(SirenMediaTypes.Json);
});

builder.Services.AddSingleton<IApplicationText, ApplicationText>();
builder.Services.AddSingleton<IApiErrorResponseFactory, ApiErrorResponseFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IEventActorContext, HttpEventActorContext>();
builder.Services.AddEventStore(builder.Configuration);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services
    .AddAuthentication(AdministratorSessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AdministratorSessionAuthenticationHandler>(
        AdministratorSessionAuthenticationHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AdministratorAuthorizationPolicies.Read,
        policy => policy.RequireRole(AdministratorRoles.Reader, AdministratorRoles.Administrator));
    options.AddPolicy(
        AdministratorAuthorizationPolicies.Write,
        policy => policy.RequireRole(AdministratorRoles.Administrator));
});
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, SirenAuthorizationMiddlewareResultHandler>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        var pathBase = builder.Configuration["PathBase"];
        if (!string.IsNullOrWhiteSpace(pathBase))
        {
            document.Servers = [new OpenApiServer { Url = pathBase }];
        }

        return Task.CompletedTask;
    });
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var english = CultureInfo.GetCultureInfo("en");
    var italian = CultureInfo.GetCultureInfo("it");
    options.DefaultRequestCulture = new RequestCulture(english);
    options.SupportedCultures = [english, italian];
    options.SupportedUICultures = [english, italian];
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
    options.ApplyCurrentCultureToResponseHeaders = true;
});

var app = builder.Build();

var pathBaseSetting = app.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBaseSetting))
{
    if (!pathBaseSetting.StartsWith('/') || pathBaseSetting.EndsWith('/') || pathBaseSetting.Contains('?') || pathBaseSetting.Contains('#'))
    {
        throw new InvalidOperationException("PathBase must be an absolute path without a trailing slash, query, or fragment.");
    }

    app.UsePathBase(pathBaseSetting);
}

app.UseRequestLocalization();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var responseFactory = httpContext.RequestServices.GetRequiredService<IApiErrorResponseFactory>();
    var (code, messageKey) = ApiErrorCodes.FromStatusCode(httpContext.Response.StatusCode);
    var response = responseFactory.Create(httpContext, httpContext.Response.StatusCode, code, messageKey);

    httpContext.Response.ContentType = SirenMediaTypes.Json;
    await SirenJson.SerializeAsync(httpContext.Response.Body, response, httpContext.RequestAborted);
});

app.MapOpenApi().AllowAnonymous();
app.MapControllers();
app.MapHealthChecks(
    "/health/ready",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await System.Text.Json.JsonSerializer.SerializeAsync(
                context.Response.Body,
                new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => new { status = entry.Value.Status.ToString(), description = entry.Value.Description })
                },
                cancellationToken: context.RequestAborted);
        }
    }).AllowAnonymous();

if (operatorCommand is not null)
{
    string userName = string.Empty;
    string password = string.Empty;
    string passwordConfirmation = string.Empty;
    string credential = string.Empty;
    try
    {
        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException("Administrator setup requires an interactive local console.");
        }

        Console.Write("Administrator account name: ");
        userName = Console.ReadLine()?.Trim() ?? string.Empty;
        password = ReadOperatorPassword("Password: ");
        passwordConfirmation = ReadOperatorPassword("Confirm password: ");
        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The password entries did not match.");
        }

        await app.Services.GetRequiredService<Marten.IDocumentStore>()
            .Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        var authenticationService = app.Services.GetRequiredService<IAdministratorAuthenticationService>();
        credential = operatorCommand == "--bootstrap-admin"
            ? await authenticationService.IssueBootstrapCredentialAsync()
            : await authenticationService.IssueRecoveryCredentialAsync();
        var result = operatorCommand == "--bootstrap-admin"
            ? await authenticationService.BootstrapAsync(credential, userName, password)
            : await authenticationService.RecoverAsync(credential, userName, password);
        if (result != AdministratorOperationResult.Succeeded)
        {
            throw new InvalidOperationException($"Administrator operation failed: {result}.");
        }

        Console.WriteLine(operatorCommand == "--bootstrap-admin"
            ? "The first administrator account was created."
            : "The administrator credentials were reset and existing sessions revoked.");
    }
    finally
    {
        credential = string.Empty;
        password = string.Empty;
        passwordConfirmation = string.Empty;
        userName = string.Empty;
        await app.DisposeAsync();
    }

    return;
}

app.Run();

static string ReadOperatorPassword(string prompt)
{
    Console.Write(prompt);
    var password = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return password.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    }
}
