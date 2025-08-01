import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface ChatMessage {
  user: string;
  message: string;
  timestamp: Date;
}

export interface CarUpdateNotification {
  carId: number;
  action: string;
  userId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: HubConnection | null = null;
  private connectionStatusSubject = new BehaviorSubject<boolean>(false);
  public connectionStatus$ = this.connectionStatusSubject.asObservable();

  // Message subjects
  private messageSubject = new Subject<ChatMessage>();
  public messages$ = this.messageSubject.asObservable();

  private carUpdateSubject = new Subject<CarUpdateNotification>();
  public carUpdates$ = this.carUpdateSubject.asObservable();

  private adminBroadcastSubject = new Subject<{title: string, body: string}>();
  public adminBroadcasts$ = this.adminBroadcastSubject.asObservable();

  constructor(private authService: AuthService) {}

  public startConnection(): void {
    if (this.hubConnection?.state === 'Connected') {
      return;
    }

    const token = this.authService.getToken();
    if (!token) {
      console.warn('No auth token available for SignalR connection');
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/socket`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR connection started');
        this.connectionStatusSubject.next(true);
        this.registerEventHandlers();
      })
      .catch(err => {
        console.error('Error starting SignalR connection: ', err);
        this.connectionStatusSubject.next(false);
      });

    this.hubConnection.onclose(() => {
      console.log('SignalR connection closed');
      this.connectionStatusSubject.next(false);
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.connectionStatusSubject.next(true);
    });
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.connectionStatusSubject.next(false);
    }
  }

  public sendMessageToAll(message: string): void {
    if (this.hubConnection?.state === 'Connected') {
      this.hubConnection.invoke('SendToAll', message)
        .catch(err => console.error('Error sending message: ', err));
    }
  }

  public sendMessageToUser(userId: string, message: string): void {
    if (this.hubConnection?.state === 'Connected') {
      this.hubConnection.invoke('SendToUser', userId, message)
        .catch(err => console.error('Error sending message to user: ', err));
    }
  }

  public joinGroup(groupName: string): void {
    if (this.hubConnection?.state === 'Connected') {
      this.hubConnection.invoke('Join', groupName)
        .catch(err => console.error('Error joining group: ', err));
    }
  }

  public leaveGroup(groupName: string): void {
    if (this.hubConnection?.state === 'Connected') {
      this.hubConnection.invoke('Leave', groupName)
        .catch(err => console.error('Error leaving group: ', err));
    }
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    // Chat messages
    this.hubConnection.on('ReceiveMessage', (user: string, message: string, timestamp: string) => {
      this.messageSubject.next({
        user,
        message,
        timestamp: new Date(timestamp)
      });
    });

    // Car updates
    this.hubConnection.on('CarUpdated', (notification: CarUpdateNotification) => {
      this.carUpdateSubject.next(notification);
    });

    // Admin broadcasts
    this.hubConnection.on('AdminBroadcast', (title: string, body: string) => {
      this.adminBroadcastSubject.next({ title, body });
    });

    // User events
    this.hubConnection.on('UserJoined', (user: string, group: string) => {
      console.log(`${user} joined ${group}`);
    });

    this.hubConnection.on('UserLeft', (user: string, group: string) => {
      console.log(`${user} left ${group}`);
    });

    this.hubConnection.on('UserOffline', (userId: string) => {
      console.log(`User ${userId} is offline`);
    });
  }
}