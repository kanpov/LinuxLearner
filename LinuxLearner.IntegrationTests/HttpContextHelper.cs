using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace LinuxLearner.IntegrationTests;

public static class HttpContextHelper
{
    public static HttpContext MakeStudentContext() => MakeContext("student");

    public static HttpContext MakeTeacherContext() => MakeContext("teacher");
    
    private static HttpContext MakeContext(string role)
    {
        var httpContext = new DefaultHttpContext();
        
        var claimsPrincipal = new ClaimsPrincipal();
        claimsPrincipal.AddIdentity(new ClaimsIdentity(
            [
                new Claim("preferred_username", role),
                new Claim("resource_access", role)
            ], "Bearer", "preferred_username", "resource_access"));

        httpContext.User = claimsPrincipal;

        return httpContext;
    }
}