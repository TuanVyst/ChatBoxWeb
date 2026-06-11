import { useState, useRef } from 'react';
import { ConnectionStatus } from '../types';
import './ChatInput.css';

interface Props {
  connectionStatus: ConnectionStatus;
  onSend: (content: string) => void;
  onUploadFile: (file: File) => Promise<void>;
}

const MAX_SIZE = 500 * 1024 * 1024;

function formatSize(bytes: number): string {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}

export default function ChatInput({ connectionStatus, onSend, onUploadFile }: Props) {
  const [text, setText] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const disabled = connectionStatus !== 'connected';

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

  const handleUpload = async () => {
    if (!selectedFile || uploading) return;
    setUploading(true);
    setError('');
    try {
      await onUploadFile(selectedFile);
      setSelectedFile(null);
      setPreview(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
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

      {error && <div className="file-error">{error}</div>}

      <div className="chat-input-bar">
        <button
          className="input-btn attach-btn"
          disabled={disabled || uploading}
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
        <button className="input-btn" disabled={disabled} title="Emoji">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" />
            <path d="M8 14s1.5 2 4 2 4-2 4-2" />
            <line x1="9" y1="9" x2="9.01" y2="9" />
            <line x1="15" y1="9" x2="15.01" y2="9" />
          </svg>
        </button>
        <button
          className="send-btn"
          onClick={handleSend}
          disabled={disabled || (!text.trim() && !selectedFile) || uploading}
          title={selectedFile ? 'Upload' : 'Send'}
        >
          {uploading ? (
            <svg className="spin" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M21 12a9 9 0 1 1-6.219-8.56" />
            </svg>
          ) : (
            <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
              <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z" />
            </svg>
          )}
        </button>
      </div>
    </div>
  );
}
