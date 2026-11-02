using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi;
using SciFiLarpNetwork.Backend.Api;
using SciFiLarpNetwork.Backend.Localization;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
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

app.Run();
