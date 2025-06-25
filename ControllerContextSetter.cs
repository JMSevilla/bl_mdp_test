using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WTW.Web;

namespace WTW.MdpService.Test;

public static class ControllerContextSetter
{
    public static void SetupControllerContext(this ControllerBase controller, string referenceNumber = "reference_number", string value = "TestReferenceNumber", bool setAuthSchemeToOpenAm = true)
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("reference_number", "TestReferenceNumber"),
            new Claim("linked_reference_number", "TestLinkedReferenceNumber"),
            new Claim("linked_business_group", "TestLinkedBusinessGroup"),
            setAuthSchemeToOpenAm?new Claim(MdpConstants.AuthSchemeClaim, MdpConstants.AuthScheme.OpenAm):new Claim(MdpConstants.AuthSchemeClaim, "WIF")
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}