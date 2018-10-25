﻿using System.Windows.Input;
using BrickController2.DeviceManagement;
using BrickController2.UI.Services.Navigation;
using BrickController2.UI.Services.Dialog;
using System.Threading.Tasks;
using BrickController2.UI.Commands;
using Device = BrickController2.DeviceManagement.Device;

namespace BrickController2.UI.ViewModels
{
    public class DevicePageViewModel : PageViewModelBase
    {
        private readonly IDeviceManager _deviceManager;
        private readonly IDialogService _dialogService;

        public DevicePageViewModel(
            INavigationService navigationService,
            IDeviceManager deviceManager,
            IDialogService dialogService,
            NavigationParameters parameters)
            : base(navigationService)
        {
            _deviceManager = deviceManager;
            _dialogService = dialogService;

            Device = parameters.Get<Device>("device");

            RenameCommand = new SafeCommand(async () => await RenameDeviceAsync());
            BuWizzOutputLevelChangedCommand = new SafeCommand<int>(outputLevel => SetBuWizzOutputLevel(outputLevel));
            BuWizz2OutputLevelChangedCommand = new SafeCommand<int>(outputLevel => SetBuWizzOutputLevel(outputLevel));
        }

        public Device Device { get; }
        public bool IsBuWizzDevice => Device.DeviceType == DeviceType.BuWizz;
        public bool IsBuWizz2Device => Device.DeviceType == DeviceType.BuWizz2;

        public ICommand RenameCommand { get; }
        public ICommand BuWizzOutputLevelChangedCommand { get; }
        public ICommand BuWizz2OutputLevelChangedCommand { get; }

        public int BuWizzOutputLevel { get; set; } = 1;
        public int BuWizz2OutputLevel { get; set; } = 1;

        public override async void OnAppearing()
        {
            if (Device.DeviceType != DeviceType.Infrared)
            {
                if (!_deviceManager.IsBluetoothOn)
                {
                    await _dialogService.ShowMessageBoxAsync("Warning", "Turn bluetooth on to connect to a bluetooth device.", "Ok");
                    await NavigationService.NavigateBackAsync();
                    return;
                }
            }

            Device.DeviceStateChanged += DeviceStateChangedHandler;
            await ConnectAsync();

            if (Device.DeviceType == DeviceType.BuWizz)
            {
                SetBuWizzOutputLevel(BuWizzOutputLevel);
            }
            else if (Device.DeviceType == DeviceType.BuWizz2)
            {
                SetBuWizzOutputLevel(BuWizz2OutputLevel);
            }

            base.OnAppearing();
        }

        public override async void OnDisappearing()
        {
            Device.DeviceStateChanged -= DeviceStateChangedHandler;
            await Device.DisconnectAsync();

            base.OnDisappearing();
        }

        private async Task RenameDeviceAsync()
        {
            var result = await _dialogService.ShowInputDialogAsync("Rename", "Enter a new name for the device", Device.Name, "Device name", "Rename", "Cancel");
            if (result.IsOk)
            {
                if (string.IsNullOrWhiteSpace(result.Result))
                {
                    await DisplayAlertAsync("Warning", "Device name can not be empty.", "Ok");
                    return;
                }

                await _dialogService.ShowProgressDialogAsync(
                    false,
                    async (progressDialog, token) => await Device.RenameDeviceAsync(Device, result.Result),
                    "Renaming...");
            }
        }

        private async Task ConnectAsync()
        {
            DeviceConnectionResult connectionResult = DeviceConnectionResult.Ok;
            await _dialogService.ShowProgressDialogAsync(
                false,
                async (progressDialog, token) =>
                {
                    connectionResult = await Device.ConnectAsync(token);
                },
                "Connecting...",
                null,
                "Cancel");

            if (connectionResult != DeviceConnectionResult.Ok)
            {
                if (connectionResult == DeviceConnectionResult.Error)
                {
                    await DisplayAlertAsync("Warning", "Failed to connect to device.", "Ok");
                }

                await NavigationService.NavigateBackAsync();
            }
        }

        private async void DeviceStateChangedHandler(object sender, DeviceStateChangedEventArgs args)
        {
            if (args.OldState == DeviceState.Connected && args.NewState == DeviceState.Disconnected && args.IsError)
            {
                var result = await _dialogService.ShowQuestionDialogAsync("Device connection lost", "Do you want to reconnect?", "Yes", "No");
                if (result)
                {
                    await ConnectAsync();
                }
                else
                {
                    await NavigationService.NavigateBackAsync();
                }
            }
        }

        private void SetBuWizzOutputLevel(int level)
        {
            Device.SetOutputLevel(level);
        }
    }
}
