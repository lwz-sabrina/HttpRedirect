using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HttpRedirect
{
    public class RedirectMiddleware : IMiddleware
    {
        private readonly RedirectOptions _options;
        private readonly ILogger<RedirectMiddleware> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RedirectMiddleware(
            IOptions<RedirectOptions> options,
            ILogger<RedirectMiddleware> logger,
            IHttpClientFactory httpClientFactory
        )
        {
            _options = options.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (_options.Filter.Invoke(context))
            {
                var uri = _options.RedirectUrl.Invoke(context);
                _logger.LogInformation("Redirecting request to {Url}", uri.ToString());
                CancellationToken cancellationToken = context.RequestAborted;
                try
                {
                    using var client = _httpClientFactory.CreateClient("redirect_httpClient");

                    var request = new HttpRequestMessage(
                        new HttpMethod(context.Request.Method),
                        uri
                    );
                    foreach (var header in context.Request.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }

                    var responseMessage = await client.SendAsync(request, cancellationToken);

                    // 将响应转发给原始请求
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    foreach (var header in responseMessage.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                    context.Response.Headers.Remove("transfer-encoding");
                    await responseMessage.Content.CopyToAsync(
                        context.Response.Body,
                        cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to redirect request to {Url}", uri);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError; // 内部服务器错误
                    await context.Response.WriteAsync(ex.ToString(), cancellationToken);
                    return;
                }
            }
            await next(context);
        }
    }
}
