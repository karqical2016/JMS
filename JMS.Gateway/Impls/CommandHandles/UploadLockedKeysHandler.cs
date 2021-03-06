﻿using JMS.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Way.Lib;
using Microsoft.Extensions.DependencyInjection;
using JMS.Dtos;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net;

namespace JMS.Impls.CommandHandles
{
    class UploadLockedKeysHandler : ICommandHandler
    {
        IServiceProvider _serviceProvider;
        LockKeyManager _lockKeyManager;
        Gateway _gateway;
        public UploadLockedKeysHandler(IServiceProvider serviceProvider)
        {
            _lockKeyManager = serviceProvider.GetService<LockKeyManager>();
            _gateway = serviceProvider.GetService<Gateway>();
        }
        public CommandType MatchCommandType => CommandType.UploadLockKeys;

        public void Handle(NetClient netclient, GatewayCommand cmd)
        {
            var keys = cmd.Content.FromJson<string[]>();
            var service = _gateway.GetServiceById(cmd.Header["ServiceId"]);
            if (service != null && service.Host == ((IPEndPoint)netclient.Socket.RemoteEndPoint).Address.ToString())
            {
                List<string> failed = new List<string>();
                foreach (var key in keys)
                {
                    if( _lockKeyManager.TryLock(key, service,false) == false)
                    {
                        failed.Add(key);
                    }
                }
                SystemEventCenter.OnMicroServiceUploadLockedKeyCompleted(service);
                netclient.WriteServiceData(new InvokeResult
                {
                    Success = true,
                    Data = failed.ToArray()
                });
            }
            else
            {
                netclient.WriteServiceData(new InvokeResult
                {
                    Success = false
                });
            }

        }
    }
}
