﻿using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Peripherals.Internals
{
    public class GattServerContext
    {
        public GattServerContext(AndroidContext context)
        {
            this.Context = context;
            this.Manager = context.GetBluetooth();
            this.Callbacks = new GattServerCallbacks();
        }

        public AndroidContext Context { get; }
        public BluetoothManager Manager { get; }
        public GattServerCallbacks Callbacks { get; }
        // subscribed device list

        public BluetoothGattServer CreateServer()
            => this.Manager.OpenGattServer(this.Context.AppContext, this.Callbacks);
    }
}
