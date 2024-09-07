﻿using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeAttribute : ActionFilterAttribute
{
    public AuthorizeAttribute()
    {

    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        try
        {
            IIdentityProvider idHolder = context.HttpContext.RequestServices.GetRequiredService(typeof(IIdentityProvider)) as IIdentityProvider;
            
            if (context.ActionDescriptor.FilterDescriptors.Where(f => f.Filter.GetType().IsAssignableTo(typeof(AnanymousAttribute))).Any())
            {
                idHolder.SetAnanymous();
            }

            if (!idHolder.IsAuthorized)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new UnauthorizedResult();
            }
        }
        catch (Exception ex)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
        }
    }

    //public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    //{
    //    try
    //    {
    //        IIdentityProvider idHolder = context.HttpContext.RequestServices.GetService(typeof(IIdentityProvider)) as IIdentityProvider;
    //        if (!idHolder.IsAuthorized)
    //        {
    //            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
    //            context.Result = new UnauthorizedResult();
    //        }
    //        else
    //        {
    //            await next();
    //        }

    //    }
    //    catch (Exception ex)
    //    {
    //        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
    //        context.Result = new UnauthorizedResult();
    //    }
    //}
}