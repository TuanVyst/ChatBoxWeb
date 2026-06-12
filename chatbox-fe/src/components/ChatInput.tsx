import { useState, useRef, useEffect } from 'react';
import { ConnectionStatus, EmojiDto } from '../types';
import { UploadProgress } from '../services/api';
import './ChatInput.css';

interface Props {
  connectionStatus: ConnectionStatus;
  onSend: (content: string) => void;
  onUploadFile: (file: File, content?: string) => Promise<void>;
  uploadProgress: UploadProgress | null;
  onCancelUpload?: () => void;
}

const MAX_SIZE = 500 * 1024 * 1024;

function formatSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}

function formatSpeed(bytesPerSec: number): string {
  if (bytesPerSec < 1024) return bytesPerSec.toFixed(0) + ' B/s';
  if (bytesPerSec < 1024 * 1024) return (bytesPerSec / 1024).toFixed(1) + ' KB/s';
  return (bytesPerSec / (1024 * 1024)).toFixed(1) + ' MB/s';
}

export default function ChatInput({ connectionStatus, onSend, onUploadFile, uploadProgress, onCancelUpload }: Props) {
  const [text, setText] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [error, setError] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const disabled = connectionStatus !== 'connected';

  const emojiRef = useRef<HTMLDivElement>(null);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [emojis, setEmojis] = useState<EmojiDto[]>([]);

  useEffect(() => {
    if (showEmojiPicker && emojis.length === 0) {
      fetch('/api/chat/emojis')
        .then(res => {
          if (!res.ok) throw new Error();
          return res.json();
        })
        .then(setEmojis)
        .catch(console.error);
    }
  }, [showEmojiPicker, emojis.length]);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (emojiRef.current && !emojiRef.current.contains(event.target as Node)) {
        setShowEmojiPicker(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const handleEmojiClick = (code: string) => {
    const input = document.querySelector('.chat-input-field') as HTMLInputElement;
    if (input) {
      const start = input.selectionStart ?? text.length;
      const end = input.selectionEnd ?? text.length;
      const newText = text.substring(0, start) + code + text.substring(end);
      setText(newText);
      
      const newCursorPos = start + code.length;
      setTimeout(() => {
        input.focus();
        input.setSelectionRange(newCursorPos, newCursorPos);
      }, 0);
    } else {
      setText(prev => prev + code);
    }
  };

  const categories = emojis.reduce((acc, emoji) => {
    if (!acc[emoji.category]) acc[emoji.category] = [];
    acc[emoji.category].push(emoji);
    return acc;
  }, {} as Record<string, typeof emojis>);

  const handleSend = () => {
    if (disabled) return;

    if (selectedFile) {
      handleUpload();
      return;
    }

    if (!text.trim()) return;
    onSend(text.trim());
    setText('');
  };

  const handleUpload = () => {
    if (!selectedFile) return;
    setError('');
    const textToSend = text.trim();
    onUploadFile(selectedFile, textToSend || undefined).catch(e => {
      setError(e instanceof Error ? e.message : 'Upload failed');
    });
    setSelectedFile(null);
    setText('');
    if (preview) URL.revokeObjectURL(preview);
    setPreview(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_SIZE) {
      setError('File too large (max 500MB)');
      return;
    }

    setError('');
    setSelectedFile(file);

    if (file.type.startsWith('image/')) {
      const url = URL.createObjectURL(file);
      setPreview(url);
    } else {
      setPreview(null);
    }
  };

  const handleRemoveFile = () => {
    setSelectedFile(null);
    if (preview) URL.revokeObjectURL(preview);
    setPreview(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="chat-input-wrapper">
      {selectedFile && (
        <div className="file-preview">
          {preview ? (
            <img src={preview} alt="Preview" className="file-preview-img" />
          ) : (
            <div className="file-preview-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
              </svg>
            </div>
          )}
          <div className="file-preview-info">
            <span className="file-preview-name">{selectedFile.name}</span>
            <span className="file-preview-size">{formatSize(selectedFile.size)}</span>
          </div>
          <button className="file-preview-remove" onClick={handleRemoveFile} title="Remove">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
      )}

      {/* Upload Progress Bar */}
      {uploadProgress && (
        <div className="upload-progress-wrapper">
          <div className="upload-progress-bar-bg">
            <div
              className="upload-progress-bar-fill"
              style={{ width: `${uploadProgress.percent}%` }}
            />
          </div>
          <div className="upload-progress-info">
            <span className="upload-progress-percent">
              {uploadProgress.percent}%
            </span>
            <span className="upload-progress-detail">
              {formatSize(uploadProgress.loaded)} / {formatSize(uploadProgress.total)}
            </span>
            <span className="upload-progress-speed">
              {formatSpeed(uploadProgress.speed)}
            </span>
            {onCancelUpload && (
              <button className="upload-progress-cancel" onClick={onCancelUpload} title="Cancel upload">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            )}
          </div>
        </div>
      )}

      {error && <div className="file-error">{error}</div>}

      <div className="chat-input-bar">
        <button
          className="input-btn attach-btn"
          disabled={disabled || !!uploadProgress}
          title="Attach file (max 500MB)"
          onClick={() => fileInputRef.current?.click()}
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
          </svg>
        </button>
        <input
          ref={fileInputRef}
          type="file"
          className="file-input-hidden"
          onChange={handleFileSelect}
          accept="image/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.zip,.rar,.7z,.txt,.csv,.json,.xml,.mp4,.avi,.mov,.mkv,.webm,.mp3,.wav,.ogg,.flac,.m4a"
        />
        <input
          className="chat-input-field"
          type="text"
          placeholder={selectedFile ? selectedFile.name : 'Message...'}
          value={text}
          onChange={e => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={disabled}
          maxLength={2000}
        />
        <div className="emoji-btn-wrapper" ref={emojiRef}>
          <button
            className={`input-btn ${showEmojiPicker ? 'active' : ''}`}
            disabled={disabled}
            title="Emoji"
            onClick={() => setShowEmojiPicker(!showEmojiPicker)}
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="10" />
              <path d="M8 14s1.5 2 4 2 4-2 4-2" />
              <line x1="9" y1="9" x2="9.01" y2="9" />
              <line x1="15" y1="9" x2="15.01" y2="9" />
            </svg>
          </button>
          
          {showEmojiPicker && (
            <div className="emoji-picker-popover">
              {Object.keys(categories).length === 0 ? (
                <div className="emoji-picker-loading">Loading...</div>
              ) : (
                <div className="emoji-picker-scroll">
                  {Object.keys(categories).map(catName => (
                    <div key={catName} className="emoji-picker-cat">
                      <div className="emoji-picker-cat-title">{catName}</div>
                      <div className="emoji-picker-cat-grid">
                        {categories[catName].map(emoji => (
                          <button
                            key={emoji.code}
                            type="button"
                            className="emoji-picker-item"
                            title={emoji.name}
                            onClick={() => handleEmojiClick(emoji.code)}
                          >
                            {emoji.code}
                          </button>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
        <button
          className="send-btn"
          onClick={handleSend}
          disabled={disabled || (!text.trim() && !selectedFile)}
          title={selectedFile ? 'Upload' : 'Send'}
        >
          <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
            <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z" />
          </svg>
        </button>
      </div>
    </div>
  );
}

