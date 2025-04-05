import { Inject, Injectable, NgZone } from '@angular/core';
import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { API_BASE_URL } from 'app/api/api';
import { BehaviorSubject, filter, map, mergeMap, Observable, ReplaySubject, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationService
{
    private serverEvents$ = new Subject<QueryChanged>();
    private hubConnection: HubConnection;
    private connectionEstablished$ = new Subject<Boolean>();
    private subscription$ = new ReplaySubject<string>();
    private subscribedPartitionId: string;
    private zone = new NgZone({ enableLongStackTrace: false });
    private _isConnected$ = new BehaviorSubject<boolean>(false);

    constructor(@Inject(API_BASE_URL) baseUrl?: string)
    {
        this.hubConnection = new HubConnectionBuilder()
            .configureLogging(LogLevel.Debug)
            .withUrl(baseUrl + '/notifications', {
                skipNegotiation: true,
                transport: HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on('Process', (queryName: string, partitionId: string, rowId: string) =>
        {
            const notification = {
                partitionId,
                queryName,
                rowId
            } as QueryChanged;
            // console.log(notification);
            // this.serverEvents$.next(notification);
            this.zone.run(() =>
            {
                this.serverEvents$.next(notification);
            });
        });

        this.hubConnection.onreconnecting(() => this._isConnected$.next(false));
        this.hubConnection.onreconnected(() =>
        {
            this.subscription$.next(this.subscribedPartitionId);
            this._isConnected$.next(true);
        });

        this.initializeSubscriptions();
        this.startConnection();

        this.serverEvents$.subscribe(
            ntf => console.log(`notification ${ntf.queryName}`)
        );
    }

    public subscribe(queryName: string): Observable<QueryChanged>
    {
        return this.serverEvents$.pipe(
            filter(ntf => ntf.queryName.toLowerCase() === queryName.toLowerCase())
        );
    }

    public switchToPartition(partitionId: string): void
    {
        this.subscribedPartitionId = partitionId;
        this.subscription$.next(partitionId);
    }

    get isConnected$()
    {
        return this._isConnected$.asObservable();
    }

    get reconnected$(): Observable<typeof RECONNECTED>
    {
        return this._isConnected$.pipe(
            filter(connected => connected),
            map(_ => RECONNECTED),
        );
    }

    public reconnect()
    {
        if (this.hubConnection.state === HubConnectionState.Disconnected)
        {
            this.startConnection();
        }
    }

    private startConnection()
    {
        this.hubConnection
            .start()
            .then(() =>
            {
                this._isConnected$.next(true);
                this.connectionEstablished$.next(true);
            })
            .catch(err =>
            {
                console.warn('Error while establishing connection: ' + err + ', retrying...');
                setTimeout(() => this.startConnection(), 5000);
            });
    }

    private initializeSubscriptions()
    {
        this.connectionEstablished$.pipe(
            filter(c => !!c),
            mergeMap(_ => this.subscription$),
        ).subscribe(name => this.hubConnection.invoke('SubscribeToPartition', name));
    }
}

export const RECONNECTED = Symbol('reconnected');

export class QueryChanged 
{
    public queryName: string;
    public partitionId: string;
    public rowId: string;
    public userId: string;
}