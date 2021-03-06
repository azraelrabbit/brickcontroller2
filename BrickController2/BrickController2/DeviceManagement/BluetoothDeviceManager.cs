﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BrickController2.Helpers;
using BrickController2.PlatformServices.BluetoothLE;

namespace BrickController2.DeviceManagement
{
    internal class BluetoothDeviceManager : IBluetoothDeviceManager
    {
        private readonly IBluetoothLEService _bleService;
        private readonly AsyncLock _asyncLock = new AsyncLock();

        public BluetoothDeviceManager(IBluetoothLEService bleService)
        {
            _bleService = bleService;
        }

        public bool IsBluetoothLESupported => _bleService.IsBluetoothLESupported;
        public bool IsBluetoothOn => _bleService.IsBluetoothOn;

        public async Task<bool> ScanAsync(Func<DeviceType, string, string, byte[], Task> deviceFoundCallback, CancellationToken token)
        {
            using (await _asyncLock.LockAsync())
            {
                if (!IsBluetoothOn)
                {
                    return true;
                }

                try
                {
                    return await _bleService.ScanDevicesAsync(
                        async scanResult =>
                        {
                            var deviceInfo = GetDeviceIfo(scanResult.AdvertismentData);
                            if (deviceInfo.DeviceType != DeviceType.Unknown)
                            {
                                await deviceFoundCallback(deviceInfo.DeviceType, scanResult.DeviceName, scanResult.DeviceAddress, deviceInfo.ManufacturerData);
                            }
                        },
                        token);
                }
                catch (OperationCanceledException)
                {
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private (DeviceType DeviceType, byte[] ManufacturerData) GetDeviceIfo(IDictionary<byte, byte[]> advertismentData)
        {
            if (advertismentData == null || !advertismentData.ContainsKey(0xFF))
            {
                return (DeviceType.Unknown, null);
            }

            var manufacturerData = advertismentData[0xFF];
            if (manufacturerData == null || manufacturerData.Length < 2)
            {
                return (DeviceType.Unknown, null);
            }

            var data1 = manufacturerData[0];
            var data2 = manufacturerData[1];

            if ((data1 & 0xFF) == 0x98 && data2 == 0x01)
            {
                return (DeviceType.SBrick, manufacturerData);
            }

            if (data1 == 0x48 && data2 == 0x4D)
            {
                return (DeviceType.BuWizz, manufacturerData);
            }

            if (data1 == 0x4e && data2 == 0x05)
            {
                return (DeviceType.BuWizz2, manufacturerData);
            }

            if ((data1 & 0xFF) == 0x97 && data2 == 0x03)
            {
                if (manufacturerData.Length >= 4)
                {
                    if (manufacturerData[3] == 0x40)
                    {
                        return (DeviceType.Boost, manufacturerData);
                    }
                    else if (manufacturerData[3] == 0x41)
                    {
                        return (DeviceType.PoweredUp, manufacturerData);
                    }
                }
            }

            return (DeviceType.Unknown, null);
        }
    }
}