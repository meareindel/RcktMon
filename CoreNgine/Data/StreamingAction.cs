﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Legacy.Models;
using Tinkoff.Trading.OpenApi.Legacy.Network;

namespace CoreNgine.Data
{
    public class BrokerAction
    {
        public Func<IConnection, Task> Action { get; private set; }
        public string Description { get; private set; }

        public BrokerAction(Func<IConnection, Task> action, string description)
        {
            Action = action;
            Description = description;
        }
    }
}
