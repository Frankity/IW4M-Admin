﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestEase;
using SharedLibraryCore;

namespace IW4MAdmin.Application.API.Master
{
    public class HeartbeatState
    {
        public bool Connected { get; set; }
    }

    public class Heartbeat
    {
        public static async Task Send(ApplicationManager mgr, bool firstHeartbeat = false)
        {
            var api = Endpoint.Get();

            if (firstHeartbeat)
            {
                var token = await api.Authenticate(new AuthenticationId()
                {
                    Id = mgr.GetApplicationSettings().Configuration().Id
                });

                api.AuthorizationToken = $"Bearer {token.AccessToken}";
            }

            var instance = new ApiInstance()
            {
                Id = mgr.GetApplicationSettings().Configuration().Id,
                Uptime = (int)(DateTime.UtcNow - mgr.StartTime).TotalSeconds,
                Version = (float)Program.Version,
                Servers = mgr.Servers.Select(s =>
                            new ApiServer()
                            {
                                ClientNum = s.ClientNum,
                                Game = s.GameName.ToString(),
                                Gametype = s.Gametype,
                                Hostname = s.Hostname,
                                Map = s.CurrentMap.Name,
                                MaxClientNum = s.MaxClients,
                                Id = s.GetHashCode(),
                                Port = (short)s.GetPort()
                            }).ToList()
            };

            if (firstHeartbeat)
            {
                var message = await api.AddInstance(instance);
            }

            else
            {
                var message = await api.UpdateInstance(instance.Id, instance);
            }
        }
    }
}
