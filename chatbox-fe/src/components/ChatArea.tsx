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
}

export default function ChatArea({ messages, currentUserId, connectionStatus, onSend, onUploadFile, uploadProgress, onCancelUpload }: Props) {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  return (
    <div className="chat-area">
      <div className="chat-header">
        <span className="chat-header-title">Chat</span>
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

