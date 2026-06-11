import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { Message, OnlineUser } from '../types';

interface SignalRCallbacks {
  onMessageReceived: (msg: Message) => void;
  onUserOnline: (userId: string, username: string) => void;
  onUserOffline: (userId: string, username: string) => void;
  onOnlineUsers: (users: OnlineUser[]) => void;
  onError: (error: string) => void;
  onStatusChange: (status: 'connected' | 'connecting' | 'disconnected') => void;
}

export function useSignalR(
  userId: string | null,
  username: string | null,
  callbacks: SignalRCallbacks
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const callbacksRef = useRef(callbacks);
  callbacksRef.current = callbacks;
  const disconnectPromiseRef = useRef<Promise<void> | null>(null);

  const connect = useCallback(async () => {
    if (!userId || !username) return;

    if (disconnectPromiseRef.current) {
      await disconnectPromiseRef.current;
    }

    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) return;

    callbacksRef.current.onStatusChange('connecting');

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/chathub')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connection.on('ReceiveMessage', (msg: Message) => {
      callbacksRef.current.onMessageReceived(msg);
    });

    connection.on('UserOnline', (userId: string, username: string) => {
      callbacksRef.current.onUserOnline(userId, username);
    });

    connection.on('UserOffline', (userId: string, username: string) => {
      callbacksRef.current.onUserOffline(userId, username);
    });

    connection.on('OnlineUsers', (users: OnlineUser[]) => {
      callbacksRef.current.onOnlineUsers(users);
    });

    connection.on('Error', (error: string) => {
      callbacksRef.current.onError(error);
    });

    connection.onreconnecting(() => {
      callbacksRef.current.onStatusChange('connecting');
    });

    connection.onreconnected(async () => {
      callbacksRef.current.onStatusChange('connected');
      try {
        await connection.invoke('JoinChat', userId, username);
      } catch {}
    });

    connection.onclose(() => {
      callbacksRef.current.onStatusChange('disconnected');
    });

    try {
      await connection.start();
      await connection.invoke('JoinChat', userId, username);
      callbacksRef.current.onStatusChange('connected');
      connectionRef.current = connection;
    } catch {
      callbacksRef.current.onStatusChange('disconnected');
    }
  }, [userId, username]);

  const disconnect = useCallback(async () => {
    const p = (async () => {
      if (connectionRef.current) {
        await connectionRef.current.stop();
        connectionRef.current = null;
      }
    })();
    disconnectPromiseRef.current = p;
    await p;
    disconnectPromiseRef.current = null;
  }, []);

  const sendMessage = useCallback(async (content: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected && userId) {
      await connectionRef.current.invoke('SendMessage', userId, content, 0, null);
    }
  }, [userId]);

  useEffect(() => {
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, []);

  return { connect, disconnect, sendMessage };
}
