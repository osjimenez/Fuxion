﻿using Fuxion.Domain.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuxion.Domain
{
    public interface IApplicationNotificationManager
    {
        Task<IDisposable> SubscribeNotificationAsync<TNotification>(Action<TNotification> action)
            where TNotification : INotification;
    }
}
