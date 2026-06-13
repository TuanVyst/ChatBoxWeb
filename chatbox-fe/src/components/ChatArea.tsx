import { useEffect, useRef } from 'react';
import { Message, ConnectionStatus } from '../types';
import { UploadProgress } from '../services/api';
import MessageItem from './MessageItem';
import ChatInput from './ChatInput';
import './ChatArea.css';

interface Props {
  messages: Message[];
  currentUserId: string;
  connectionStatus: ConnectionStatus;
  onSend: (content: string) => void;
  onUploadFile: (file: File, content?: string) => Promise<void>;
  uploadProgress: UploadProgress | null;
  onCancelUpload?: () => void;
  showOnlineList: boolean;
  setShowOnlineList: (show: boolean) => void;
  showMedia: boolean;
  setShowMedia: (show: boolean) => void;
}

export default function ChatArea({
  messages,
  currentUserId,
  connectionStatus,
  onSend,
  onUploadFile,
  uploadProgress,
  onCancelUpload,
  showOnlineList,
  setShowOnlineList,
  showMedia,
  setShowMedia,
}: Props) {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  return (
    <div className="chat-area">
      <div className="chat-header">
        <span className="chat-header-title">Chat</span>
        {currentUserId && (
          <div className="chat-header-actions">
            <button
              className={`header-toggle-btn ${showOnlineList ? 'active' : ''}`}
              onClick={() => setShowOnlineList(!showOnlineList)}
              title={showOnlineList ? "Ẩn danh sách online" : "Hiện danh sách online"}
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                <circle cx="9" cy="7" r="4" />
                <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                <path d="M16 3.13a4 4 0 0 1 0 7.75" />
              </svg>
            </button>
            <button
              className={`header-toggle-btn ${showMedia ? 'active' : ''}`}
              onClick={() => setShowMedia(!showMedia)}
              title={showMedia ? "Ẩn media" : "Hiện media"}
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                <circle cx="8.5" cy="8.5" r="1.5" />
                <polyline points="21 15 16 10 5 21" />
              </svg>
            </button>
          </div>
        )}
      </div>

      <div className="messages-container">
        {messages.length === 0 && (
          <div className="messages-empty">
            <div className="messages-empty-icon">#</div>
            <h2>Welcome to ChatBox!</h2>
            <p>Say something to start the conversation.</p>
          </div>
        )}
        {messages.map(msg => (
          <MessageItem
            key={msg.id}
            message={msg}
            isOwn={msg.senderId === currentUserId}
          />
        ))}
        <div ref={messagesEndRef} />
      </div>

      <ChatInput
        connectionStatus={connectionStatus}
        onSend={onSend}
        onUploadFile={onUploadFile}
        uploadProgress={uploadProgress}
        onCancelUpload={onCancelUpload}
      />
    </div>
  );
}

