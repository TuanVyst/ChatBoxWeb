import { useState, useEffect, useCallback } from 'react';
import { Message, OnlineUser, ConnectionStatus } from './types';
import { loginUser, getChatHistory, uploadFile } from './services/api';
import { useSignalR } from './hooks/useSignalR';
import SidebarLeft from './components/SidebarLeft';
import ChatArea from './components/ChatArea';
import SidebarRight from './components/SidebarRight';
import './App.css';

function App() {
  const [user, setUser] = useState<{ id: string; username: string } | null>(() => {
    const saved = localStorage.getItem('chatbox_user');
    return saved ? JSON.parse(saved) : null;
  });
  const [messages, setMessages] = useState<Message[]>([]);
  const [onlineUsers, setOnlineUsers] = useState<OnlineUser[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected');

  useEffect(() => {
    if (user) {
      getChatHistory().then(setMessages).catch(console.error);
    } else {
      setMessages([]);
    }
  }, [user]);

  const onMessageReceived = useCallback((msg: Message) => {
    setMessages(prev => prev.some(m => m.id === msg.id) ? prev : [...prev, msg]);
  }, []);

  const onUserOnline = useCallback(() => {}, []);

  const onUserOffline = useCallback((_userId: string, username: string) => {
    const logMsg: Message = {
      id: `log-disc-${Date.now()}-${Math.random()}`,
      senderId: '00000000-0000-0000-0000-000000000000',
      senderUsername: '',
      content: `${username} disconnected`,
      type: 0,
      fileUrl: null,
      timestamp: new Date().toISOString(),
    };
    setMessages(prev => [...prev, logMsg]);
  }, []);

  const onOnlineUsers = useCallback((users: OnlineUser[]) => {
    const deduped = users.filter((u, i, arr) => arr.findIndex(x => x.userId === u.userId) === i);
    setOnlineUsers(deduped);
  }, []);

  const onError = useCallback((error: string) => {
    console.error('SignalR error:', error);
  }, []);

  const { connect, disconnect, sendMessage } = useSignalR(
    user?.id ?? null,
    user?.username ?? null,
    { onMessageReceived, onUserOnline, onUserOffline, onOnlineUsers, onError, onStatusChange: setConnectionStatus }
  );

  useEffect(() => {
    if (user) connect();
    return () => { disconnect(); };
  }, [user, connect, disconnect]);

  const handleLogin = async (username: string) => {
    const u = await loginUser(username);
    const userData = { id: u.id, username: u.username };
    localStorage.setItem('chatbox_user', JSON.stringify(userData));
    setUser(userData);
  };

  const handleLogout = () => {
    disconnect();
    localStorage.removeItem('chatbox_user');
    setUser(null);
    setMessages([]);
    setOnlineUsers([]);
    setConnectionStatus('disconnected');
  };

  const handleUploadFile = async (file: File) => {
    if (!user) return;
    await uploadFile(user.id, file);
  };

  const fileMessages = messages.filter(m => m.type === 1 || m.type === 2);

  return (
    <div className="app-layout">
      <SidebarLeft
        user={user}
        onlineUsers={onlineUsers}
        connectionStatus={connectionStatus}
        onLogin={handleLogin}
        onConnect={connect}
        onLogout={handleLogout}
      />
      <ChatArea
        messages={messages}
        currentUserId={user?.id ?? ''}
        connectionStatus={connectionStatus}
        onSend={sendMessage}
        onUploadFile={handleUploadFile}
      />
      <SidebarRight
        fileMessages={fileMessages}
      />
    </div>
  );
}

export default App;
