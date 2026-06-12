import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { Message, OnlineUser } from '../types';
import { API_URL } from '../services/api';

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
  const disconnectPromiseRef = useRef<Promise<void> | null>(null);

  callbacksRef.current = callbacks;

  const connect = useCallback(async () => {
    if (!userId || !username) return;

    if (disconnectPromiseRef.current) {
      await disconnectPromiseRef.current;
    }

    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    callbacksRef.current.onStatusChange('connecting');

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/chathub`)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    connection.on('ReceiveMessage', (msg: Message) => {
      callbacksRef.current.onMessageReceived(msg);
    });

    connection.on('UserOnline', (onlineUserId: string, onlineUsername: string) => {
      callbacksRef.current.onUserOnline(onlineUserId, onlineUsername);
    });

    connection.on('UserOffline', (offlineUserId: string, offlineUsername: string) => {
      callbacksRef.current.onUserOffline(offlineUserId, offlineUsername);
    });

    connection.on('OnlineUsers', (users: OnlineUser[]) => {
      callbacksRef.current.onOnlineUsers(users);
    });

    connection.on('Error', (error: string) => {
      callbacksRef.current.onError(error);
      console.error('SignalR server error:', error);
    });

    connection.onreconnecting(() => {
      callbacksRef.current.onStatusChange('connecting');
    });

    connection.onreconnected(async () => {
      callbacksRef.current.onStatusChange('connected');

      try {
        await connection.invoke('JoinChat', userId, username);
      } catch (error) {
        console.error('JoinChat after reconnect failed:', error);
      }
    });

    connection.onclose(() => {
      callbacksRef.current.onStatusChange('disconnected');
    });

    try {
      await connection.start();
      connectionRef.current = connection;

      await connection.invoke('JoinChat', userId, username);

      callbacksRef.current.onStatusChange('connected');
    } catch (error) {
      console.error('SignalR connect failed:', error);
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

  const sendMessage = useCallback(
    async (content: string) => {
      if (!userId) {
        callbacksRef.current.onError('UserId không tồn tại');
        return;
      }

      if (!content.trim()) {
        return;
      }

      if (connectionRef.current?.state !== signalR.HubConnectionState.Connected) {
        callbacksRef.current.onError('Chưa kết nối tới server');
        return;
      }

      try {
        await connectionRef.current.invoke(
          'SendMessage',
          userId,
          content,
          0,
          null
        );
      } catch (error) {
        console.error('SendMessage failed:', error);
        callbacksRef.current.onError('Gửi tin nhắn thất bại');
      }
    },
    [userId]
  );

  useEffect(() => {
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, []);

  return { connect, disconnect, sendMessage };
}