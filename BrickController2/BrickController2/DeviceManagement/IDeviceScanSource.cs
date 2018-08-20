﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrickController2.DeviceManagement
{
    public interface IDeviceScanSource
    {
        Task ScanAsync(Func<Device, Task> deviceFoundCallback, CancellationToken token);
    }
}
