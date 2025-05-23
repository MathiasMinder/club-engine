﻿using System.IdentityModel.Tokens.Jwt;

using AppEngine.Authentication.Users;
using AppEngine.Authorization.UsersInPartition;

using Microsoft.AspNetCore.Http;

namespace AppEngine.Authentication;

public class GoogleIdentityProvider : IIdentityProvider
{
    public const string HeaderKeyIdToken = "X-MS-TOKEN-GOOGLE-ID-TOKEN";

    public IdentityProvider Provider => IdentityProvider.Google;

    public (IdentityProvider Provider, string Identifier)? GetIdentifier(IHttpContextAccessor contextAccessor)
    {
        var idTokenString = contextAccessor.HttpContext?.Request.Headers[HeaderKeyIdToken].FirstOrDefault();
        if (idTokenString != null)
        {
            var token = new JwtSecurityToken(idTokenString);
            return (IdentityProvider.Google, token.Subject);
        }

        return null;
    }

    public AuthenticatedUser GetUser(IHttpContextAccessor contextAccessor)
    {
        var headers = contextAccessor.HttpContext?.Request.Headers;
        var idTokenString = headers?[HeaderKeyIdToken].FirstOrDefault();
        if (idTokenString != null)
        {
            var token = new JwtSecurityToken(idTokenString);

            var firstName = token.GetClaim("given_name");
            var lastName = token.GetClaim("family_name");
            var email = token.GetClaim("email");
            var avatarUrl = token.GetClaim("picture");
            return new AuthenticatedUser(Provider, token.Subject, firstName, lastName, email, avatarUrl);
        }

        return AuthenticatedUser.None;
    }

    public Task<ExternalUserDetails?> GetUserDetails(string identifier)
    {
        return Task.FromResult((ExternalUserDetails?)null);
    }
}