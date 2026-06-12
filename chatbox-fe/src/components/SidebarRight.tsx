import { useState } from 'react';
import { Message, MessageType } from '../types';
import './SidebarRight.css';

interface Props {
  fileMessages: Message[];
}

function getFileName(url: string): string {
  const parts = url.split('/');
  return decodeURIComponent(parts[parts.length - 1] || url);
}

export default function SidebarRight({ fileMessages }: Props) {
  const [mediaExpanded, setMediaExpanded] = useState(true);
  const [showAllImages, setShowAllImages] = useState(false);

  const reversedFiles = [...fileMessages].reverse();
  const images = reversedFiles.filter(m => m.type === MessageType.Image);
  const docs = reversedFiles.filter(m => m.type === MessageType.File);

  const visibleImages = showAllImages ? images : images.slice(0, 6);

  return (
    <div className="sidebar-right">
      <div className="media-section">
        <div className="media-header" onClick={() => setMediaExpanded(!mediaExpanded)}>
          <svg className={`media-arrow ${mediaExpanded ? 'expanded' : ''}`} width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
            <path d="M8 5l8 7-8 7" />
          </svg>
          <span>Shared Media</span>
          <span className="media-count">{fileMessages.length}</span>
        </div>

        {mediaExpanded && (
          <div className="media-content">
            {images.length > 0 && (
              <div className="media-category">
                <div className="media-category-header">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>
                  <span>Photos</span>
                  <span className="media-category-count">{images.length}</span>
                </div>
                <div className="media-thumbnails">
                  {visibleImages.map(msg => (
                    <a key={msg.id} href={msg.fileUrl!} target="_blank" rel="noopener noreferrer" className="media-thumb">
                      <img src={msg.fileUrl!} alt="" />
                    </a>
                  ))}
                  {images.length > 6 && !showAllImages && (
                    <div className="media-more" onClick={() => setShowAllImages(true)}>
                      +{images.length - 6}
                    </div>
                  )}
                </div>
                {showAllImages && (
                  <div className="media-toggle-less" onClick={() => setShowAllImages(false)}>
                    Show less
                  </div>
                )}
              </div>
            )}

            {docs.length > 0 && (
              <div className="media-category">
                <div className="media-category-header">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                  <span>Files</span>
                  <span className="media-category-count">{docs.length}</span>
                </div>
                <div className="media-docs">
                  {docs.map(msg => (
                    <a key={msg.id} href={msg.fileUrl!} download className="media-doc-item">
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                      <span className="media-doc-name">{msg.originalFileName || getFileName(msg.fileUrl!)}</span>
                    </a>
                  ))}
                </div>
              </div>
            )}

            {fileMessages.length === 0 && (
              <div className="media-empty">No shared files yet</div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
