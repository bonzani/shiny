﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


public partial class Peripheral : CBPeripheralDelegate, IPeripheral
{
    readonly BleManager manager;
    readonly ILogger logger;
    IDisposable? autoReconnectSub;


    public Peripheral(
        BleManager manager,
        CBPeripheral native,
        ILogger<IPeripheral> logger
    )
    {
        this.manager = manager;
        this.Native = native;
        this.logger = logger;

        this.Uuid = native.Identifier.ToString();
        this.Native.Delegate = this;
    }


    public CBPeripheral Native { get; }

    public string Uuid { get; }
    public string? Name => this.Native.Name;
    public int Mtu => (int)this
        .Native
        .GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse);

    public ConnectionState Status => this.Native.State switch
    {
        CBPeripheralState.Connected => ConnectionState.Connected,
        CBPeripheralState.Connecting => ConnectionState.Connecting,
        CBPeripheralState.Disconnected => ConnectionState.Disconnected,
        CBPeripheralState.Disconnecting => ConnectionState.Disconnecting,
        _ => ConnectionState.Disconnected
    };


    public void CancelConnection()
    {
        this.autoReconnectSub?.Dispose();
        this.manager.Manager.CancelPeripheralConnection(this.Native);
    }


    public void Connect(ConnectionConfig? config = null)
    {
        var arc = config?.AutoConnect ?? true;
        if (arc)
        {
            this.autoReconnectSub = this
                .WhenDisconnected()
                .Skip(1)
                .Subscribe(_ => this.DoConnect());
        }
        this.DoConnect();
    }


    protected void DoConnect() => this.manager
        .Manager
        .ConnectPeripheral(this.Native, new PeripheralConnectionOptions
        {
            NotifyOnDisconnection = true,
            NotifyOnConnection = true,
            NotifyOnNotification = true
        });


    public IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
    {
        var sub = this.rssiSubj.Subscribe(x =>
        {
            if (x.Exception == null)
                ob.OnNext(x.Rssi);
            else
                ob.OnError(x.Exception);
        });
        this.Native.ReadRSSI();

        return sub;
    });

    readonly Subject<(int Rssi, InvalidOperationException? Exception)> rssiSubj = new();
    public override void RssiRead(CBPeripheral peripheral, NSNumber rssi, NSError? error)
    {
        if (error == null)
            this.rssiSubj.OnNext((rssi.Int32Value, null));
        else
            this.rssiSubj.OnNext((0, new InvalidOperationException(error.LocalizedDescription)));
    }
    //public override void RssiUpdated(CBPeripheral peripheral, NSError? error) {}



    public IObservable<ConnectionState> WhenStatusChanged() => Observable.Create<ConnectionState>(ob =>
    {
        ob.OnNext(this.Status);
        var sub = this.ConnectionSubject.Subscribe(ob.OnNext);

        //    //this.context
        //    //    .FailedConnection
        //    //    .Where(x => x.Equals(this.peripheral))
        //    //    .Subscribe(x => ob.OnNext(ConnectionStatus.Failed));

        return () => sub.Dispose();
    });


    internal Subject<ConnectionState> ConnectionSubject { get; } = new();

    //public override IObservable<BleException> WhenConnectionFailed() => this.context
    //    .FailedConnection
    //    .Where(x => x.Peripheral.Equals(this.Native))
    //    .Select(x => new BleException(x.Error.ToString()));
    //public override void UpdatedName(CBPeripheral peripheral)
    //{
    //}
}