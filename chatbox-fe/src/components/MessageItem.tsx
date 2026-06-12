import { Message as MessageType, MessageType as MsgTypeEnum } from '../types';
import { API_URL } from '../services/api';
import UserAvatar from './UserAvatar';
import './MessageItem.css';

interface Props {
  message: MessageType;
  isOwn: boolean;
}

const SYSTEM_ID = '00000000-0000-0000-0000-000000000000';

function formatTime(timestamp: string): string {
  const d = new Date(timestamp);
  return d.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'Asia/Ho_Chi_Minh',
  });
}

function getFileUrl(fileUrl?: string | null): string {
  if (!fileUrl) return '';
  if (fileUrl.startsWith('http')) return fileUrl;
  return `${API_URL}${fileUrl}`;
}

export default function MessageItem({ message, isOwn }: Props) {
  if (message.senderId === SYSTEM_ID) {
    return (
      <div className="message-system-log">
        <span className="message-system-log-text">{message.content}</span>
      </div>
    );
  }

  const fullFileUrl = getFileUrl(message.fileUrl);

  return (
    <div className={`message-item ${isOwn ? 'own' : ''}`}>
      <UserAvatar username={message.senderUsername} size={36} />

      <div className="message-content">
        <div className="message-header">
          <span className="message-sender">{message.senderUsername}</span>
          <span className="message-time">{formatTime(message.timestamp)}</span>
        </div>

        {message.content && (
          <div className="message-body">{message.content}</div>
        )}

        {message.type === MsgTypeEnum.Image && message.fileUrl && (
          <a
            href={fullFileUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="message-image-link"
          >
            <img
              src={fullFileUrl}
              alt="Image"
              className="message-image"
            />
          </a>
        )}

        {message.type === MsgTypeEnum.File && message.fileUrl && (
          <div className="message-file-box">
            <a
              href={fullFileUrl}
              download
              className="message-file-link"
            >
              <svg
                width="24"
                height="24"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
                <line x1="16" y1="13" x2="8" y2="13" />
                <line x1="16" y1="17" x2="8" y2="17" />
              </svg>

              <span className="message-file-name">
                {message.originalFileName || message.fileUrl.split('/').pop()}
              </span>
            </a>

            <a
              href={fullFileUrl}
              download
              className="message-file-download"
              title="Download"
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="7 10 12 15 17 10" />
                <line x1="12" y1="15" x2="12" y2="3" />
              </svg>
            </a>
          </div>
        )}
      </div>
    </div>
  );
}