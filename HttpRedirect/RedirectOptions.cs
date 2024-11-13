using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace HttpRedirect
{
    public class RedirectOptions
    {
        public Func<HttpContext, bool> Filter { get; set; } = _ => false;
        public Func<HttpContext, Uri> RedirectUrl { get; set; } =
            _ => new Uri(_.Request.GetDisplayUrl());
    }
}
