﻿using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;
using System;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;

namespace Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);

            // Token Generation
            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/Token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
                Provider = new SimpleAuthorizationServerProvider()
            });

            // Token Consumption
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}
