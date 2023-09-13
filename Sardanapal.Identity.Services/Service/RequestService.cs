using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sardanapal.Identity.Services.Service
{
    public interface IRequestService
    {
        string Token { get; set; }
    }

    public class RequestService : IRequestService
    {
        public string Token { get; set; }

        public RequestService(IHttpContextAccessor _http)
        {
            Token = _http.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
        }
    }
}
