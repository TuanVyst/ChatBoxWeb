import { useState, useEffect, useCallback } from 'react';
import { Message, OnlineUser, ConnectionStatus, MessageType } from './types';
import { loginUser, getChatHistory, uploadFile, UploadProgress } from './services/api';
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
  const [connectionStatus, setConnectionStatus] =
    useState<ConnectionStatus>('disconnected');
  const [uploadProgress, setUploadProgress] = useState<UploadProgress | null>(null);
  const [abortController, setAbortController] = useState<AbortController | null>(null);
  const [showOnlineList, setShowOnlineList] = useState(true);
  const [showMedia, setShowMedia] = useState(true);

  useEffect(() => {
    if (user) {
      getChatHistory().then(setMessages).catch(console.error);
    } else {
      setMessages([]);
    }
  }, [user]);

  const onMessageReceived = useCallback((msg: Message) => {
    setMessages(prev =>
      prev.some(m => m.id === msg.id) ? prev : [...prev, msg]
    );
  }, []);

  const onUserOnline = useCallback(() => {}, []);

  const onUserOffline = useCallback((_userId: string, username: string) => {
    const logMsg: Message = {
      id: `log-disc-${Date.now()}-${Math.random()}`,
      senderId: '00000000-0000-0000-0000-000000000000',
      senderUsername: '',
      content: `${username} disconnected`,
      type: MessageType.Text,
      fileUrl: null,
      originalFileName: null,
      timestamp: new Date().toISOString(),
    };

    setMessages(prev => [...prev, logMsg]);
  }, []);

  const onOnlineUsers = useCallback((users: OnlineUser[]) => {
    const deduped = users.filter(
      (u, i, arr) => arr.findIndex(x => x.userId === u.userId) === i
    );

    setOnlineUsers(deduped);
  }, []);

  const onError = useCallback((error: string) => {
    console.error('SignalR error:', error);
  }, []);

  const { connect, disconnect, sendMessage } = useSignalR(
    user?.id ?? null,
    user?.username ?? null,
    {
      onMessageReceived,
      onUserOnline,
      onUserOffline,
      onOnlineUsers,
      onError,
      onStatusChange: setConnectionStatus,
    }
  );

  useEffect(() => {
    if (user) connect();

    return () => {
      disconnect();
    };
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

  const handleCancelUpload = () => {
    if (abortController) {
      abortController.abort();
      setAbortController(null);
      setUploadProgress(null);
    }
  };

  const handleUploadFile = async (file: File, content?: string) => {
    if (!user) return;

    const controller = new AbortController();
    setAbortController(controller);
    setUploadProgress({ percent: 0, loaded: 0, total: file.size, speed: 0 });

    try {
      const uploadedMessage = await uploadFile(user.id, file, content, (progress) => {
        setUploadProgress(progress);
      }, controller.signal);

      console.log('Uploaded message:', uploadedMessage);

      setMessages(prev =>
        prev.some(m => m.id === uploadedMessage.id)
          ? prev
          : [...prev, uploadedMessage]
      );

      const history = await getChatHistory();
      setMessages(history);
    } catch (error: any) {
      if (error.message === 'Upload cancelled') {
        console.log('Upload cancelled by user');
      } else {
        console.error('Upload file failed:', error);
      }
    } finally {
      setAbortController(null);
      if (controller.signal.aborted) {
        setUploadProgress(null);
      } else {
        setTimeout(() => setUploadProgress(null), 1000);
      }
    }
  };

  const fileMessages = messages.filter(
    m => m.type === MessageType.Image || m.type === MessageType.File
  );

  return (
    <div className="app-layout">
      <SidebarLeft
        user={user}
        onlineUsers={onlineUsers}
        connectionStatus={connectionStatus}
        onLogin={handleLogin}
        onConnect={connect}
        onLogout={handleLogout}
        showOnlineList={showOnlineList}
      />

      <ChatArea
        messages={messages}
        currentUserId={user?.id ?? ''}
        connectionStatus={connectionStatus}
        onSend={sendMessage}
        onUploadFile={handleUploadFile}
        uploadProgress={uploadProgress}
        onCancelUpload={handleCancelUpload}
        showOnlineList={showOnlineList}
        setShowOnlineList={setShowOnlineList}
        showMedia={showMedia}
        setShowMedia={setShowMedia}
      />

      {showMedia && <SidebarRight fileMessages={fileMessages} />}
    </div>
  );
}

export default App;