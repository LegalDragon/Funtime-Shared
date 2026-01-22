import { useState, useRef, useEffect, useCallback } from 'react';
import { Upload, X, Loader2, Image, FileText, File, Video, Music, Link2, ExternalLink, AlertCircle } from 'lucide-react';
import { assetApi, type AssetUploadResponse, type AssetType, type AssetFileTypesResponse, type AssetFileType } from '../utils/api';

type UploadMode = 'file' | 'link';

interface AssetUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUploadComplete: (asset: AssetUploadResponse) => void;
  category?: string;
  siteKey?: string;
  acceptedTypes?: string; // Override - if provided, disables dynamic loading
  maxSizeMB?: number; // Fallback if no dynamic types
  title?: string;
  allowExternalLinks?: boolean;
  defaultAssetType?: AssetType;
}

const ASSET_TYPE_OPTIONS: { value: AssetType; label: string; icon: typeof Image }[] = [
  { value: 'image', label: 'Image', icon: Image },
  { value: 'video', label: 'Video', icon: Video },
  { value: 'document', label: 'Document', icon: FileText },
  { value: 'audio', label: 'Audio', icon: Music },
  { value: 'link', label: 'Link', icon: Link2 },
];

// Default fallback types if API fails
const DEFAULT_ACCEPT_TYPES = 'image/*,video/*,audio/*,.pdf,.doc,.docx,.md,.html,.htm';
const DEFAULT_MAX_SIZE_MB = 10;

// Extract YouTube video ID from URL
function extractYouTubeId(url: string): string | null {
  try {
    const urlObj = new URL(url);
    if (urlObj.hostname.includes('youtube.com')) {
      return urlObj.searchParams.get('v');
    }
    if (urlObj.hostname.includes('youtu.be')) {
      return urlObj.pathname.slice(1);
    }
  } catch {
    return null;
  }
  return null;
}

// Get thumbnail URL for video platforms
function getVideoThumbnail(url: string): string | null {
  const youtubeId = extractYouTubeId(url);
  if (youtubeId) {
    return `https://img.youtube.com/vi/${youtubeId}/hqdefault.jpg`;
  }
  return null;
}

// Detect if URL is a video platform
function isVideoUrl(url: string): boolean {
  try {
    const urlObj = new URL(url);
    const host = urlObj.hostname.toLowerCase();
    return host.includes('youtube.com') || host.includes('youtu.be') || host.includes('vimeo.com');
  } catch {
    return false;
  }
}

export function AssetUploadModal({
  isOpen,
  onClose,
  onUploadComplete,
  category,
  siteKey,
  acceptedTypes, // If provided, skip dynamic loading
  maxSizeMB = DEFAULT_MAX_SIZE_MB,
  title = 'Upload Asset',
  allowExternalLinks = true,
  defaultAssetType,
}: AssetUploadModalProps) {
  const [uploadMode, setUploadMode] = useState<UploadMode>('file');
  const [assetType, setAssetType] = useState<AssetType>(defaultAssetType || 'image');
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dragOver, setDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Dynamic file types state
  const [fileTypesData, setFileTypesData] = useState<AssetFileTypesResponse | null>(null);
  const [loadingFileTypes, setLoadingFileTypes] = useState(false);
  const [fileTypesError, setFileTypesError] = useState<string | null>(null);

  // External link state
  const [externalUrl, setExternalUrl] = useState('');
  const [linkTitle, setLinkTitle] = useState('');
  const [linkThumbnail, setLinkThumbnail] = useState<string | null>(null);

  // Fetch file types when modal opens
  useEffect(() => {
    if (isOpen && !acceptedTypes && !fileTypesData && !loadingFileTypes) {
      setLoadingFileTypes(true);
      setFileTypesError(null);

      assetApi.getEnabledFileTypes()
        .then(data => {
          setFileTypesData(data);
        })
        .catch(err => {
          console.warn('Failed to load file types, using defaults:', err);
          setFileTypesError('Using default file types');
        })
        .finally(() => {
          setLoadingFileTypes(false);
        });
    }
  }, [isOpen, acceptedTypes, fileTypesData, loadingFileTypes]);

  // Get accept string for file input
  const getAcceptString = useCallback(() => {
    if (acceptedTypes) return acceptedTypes;
    if (fileTypesData?.acceptString) return fileTypesData.acceptString;
    return DEFAULT_ACCEPT_TYPES;
  }, [acceptedTypes, fileTypesData]);

  // Get max size for a specific mime type
  const getMaxSizeForMimeType = useCallback((mimeType: string): number => {
    if (!fileTypesData?.fileTypes) return maxSizeMB;

    const matchingType = fileTypesData.fileTypes.find(
      ft => ft.mimeType.toLowerCase() === mimeType.toLowerCase()
    );

    return matchingType?.maxSizeMB ?? maxSizeMB;
  }, [fileTypesData, maxSizeMB]);

  // Get display info for file validation
  const getFileTypeInfo = useCallback((mimeType: string): AssetFileType | null => {
    if (!fileTypesData?.fileTypes) return null;
    return fileTypesData.fileTypes.find(
      ft => ft.mimeType.toLowerCase() === mimeType.toLowerCase()
    ) ?? null;
  }, [fileTypesData]);

  // Auto-detect video URLs and get thumbnail
  useEffect(() => {
    if (externalUrl) {
      if (isVideoUrl(externalUrl)) {
        setAssetType('video');
        const thumbnail = getVideoThumbnail(externalUrl);
        setLinkThumbnail(thumbnail);
      } else {
        setLinkThumbnail(null);
      }
    }
  }, [externalUrl]);

  const handleFileSelect = (selectedFile: File) => {
    setError(null);

    // Get max size for this file type
    const fileMaxSizeMB = getMaxSizeForMimeType(selectedFile.type);
    const maxBytes = fileMaxSizeMB * 1024 * 1024;

    if (selectedFile.size > maxBytes) {
      const fileTypeInfo = getFileTypeInfo(selectedFile.type);
      const typeName = fileTypeInfo?.displayName ?? selectedFile.type;
      setError(`File size must be less than ${fileMaxSizeMB}MB for ${typeName}`);
      return;
    }

    setFile(selectedFile);

    // Auto-detect asset type from file
    if (selectedFile.type.startsWith('image/')) {
      setAssetType('image');
    } else if (selectedFile.type.startsWith('video/')) {
      setAssetType('video');
    } else if (selectedFile.type.startsWith('audio/')) {
      setAssetType('audio');
    } else {
      setAssetType('document');
    }

    // Generate preview for images and videos
    if (selectedFile.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onloadend = () => {
        setPreview(reader.result as string);
      };
      reader.readAsDataURL(selectedFile);
    } else if (selectedFile.type.startsWith('video/')) {
      const videoUrl = URL.createObjectURL(selectedFile);
      setPreview(videoUrl);
    } else {
      setPreview(null);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);

    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile) {
      handleFileSelect(droppedFile);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      handleFileSelect(selectedFile);
    }
  };

  const handleUpload = async () => {
    setUploading(true);
    setError(null);

    try {
      let result: AssetUploadResponse;

      if (uploadMode === 'link') {
        // Register external link
        if (!externalUrl) {
          setError('Please enter a URL');
          setUploading(false);
          return;
        }

        result = await assetApi.registerLink({
          url: externalUrl,
          title: linkTitle || undefined,
          assetType: assetType,
          thumbnailUrl: linkThumbnail || undefined,
          category,
          siteKey,
          isPublic: true,
        });
      } else {
        // Upload file
        if (!file) {
          setError('Please select a file');
          setUploading(false);
          return;
        }

        result = await assetApi.upload(file, {
          assetType,
          category,
          siteKey,
        });
      }

      onUploadComplete(result);
      handleClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleClose = () => {
    setFile(null);
    setPreview(null);
    setError(null);
    setDragOver(false);
    setExternalUrl('');
    setLinkTitle('');
    setLinkThumbnail(null);
    setUploadMode('file');
    onClose();
  };

  const getFileIcon = () => {
    if (!file) return <Upload className="w-12 h-12 text-gray-400" />;

    if (file.type.startsWith('image/')) {
      return <Image className="w-12 h-12 text-blue-500" />;
    } else if (file.type.startsWith('video/')) {
      return <Video className="w-12 h-12 text-purple-500" />;
    } else if (file.type.startsWith('audio/')) {
      return <Music className="w-12 h-12 text-green-500" />;
    } else if (file.type.includes('pdf')) {
      return <FileText className="w-12 h-12 text-red-500" />;
    }
    return <File className="w-12 h-12 text-gray-500" />;
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  // Get display max size (varies by category when using dynamic types)
  const getDisplayMaxSize = (): string => {
    if (!fileTypesData?.byCategory) return `${maxSizeMB}MB`;

    // Get unique max sizes
    const sizes = new Set(fileTypesData.fileTypes.map(ft => ft.maxSizeMB));
    if (sizes.size === 1) {
      return `${Array.from(sizes)[0]}MB`;
    }

    // Show range
    const min = Math.min(...Array.from(sizes));
    const max = Math.max(...Array.from(sizes));
    return `${min}-${max}MB`;
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b">
          <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
          <button
            onClick={handleClose}
            className="p-1 hover:bg-gray-100 rounded-full transition-colors"
            disabled={uploading}
          >
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        {/* Content */}
        <div className="p-4 space-y-4">
          {/* Loading file types indicator */}
          {loadingFileTypes && (
            <div className="flex items-center justify-center py-2 text-sm text-gray-500">
              <Loader2 className="w-4 h-4 animate-spin mr-2" />
              Loading file types...
            </div>
          )}

          {/* File types error (non-blocking) */}
          {fileTypesError && !loadingFileTypes && (
            <div className="flex items-center text-xs text-amber-600 bg-amber-50 px-3 py-1.5 rounded">
              <AlertCircle className="w-3 h-3 mr-1.5 flex-shrink-0" />
              {fileTypesError}
            </div>
          )}

          {/* Upload Mode Tabs */}
          {allowExternalLinks && (
            <div className="flex border-b border-gray-200">
              <button
                onClick={() => setUploadMode('file')}
                className={`flex-1 py-2 px-4 text-sm font-medium border-b-2 transition-colors ${
                  uploadMode === 'file'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                <Upload className="w-4 h-4 inline-block mr-2" />
                Upload File
              </button>
              <button
                onClick={() => setUploadMode('link')}
                className={`flex-1 py-2 px-4 text-sm font-medium border-b-2 transition-colors ${
                  uploadMode === 'link'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                <ExternalLink className="w-4 h-4 inline-block mr-2" />
                External Link
              </button>
            </div>
          )}

          {/* Asset Type Dropdown */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Asset Type</label>
            <select
              value={assetType}
              onChange={(e) => setAssetType(e.target.value as AssetType)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {ASSET_TYPE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          {uploadMode === 'file' ? (
            <>
              {/* Drop zone */}
              <div
                className={`
                  border-2 border-dashed rounded-lg p-8 text-center transition-colors cursor-pointer
                  ${dragOver ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'}
                  ${file ? 'bg-gray-50' : ''}
                `}
                onDragOver={(e) => {
                  e.preventDefault();
                  setDragOver(true);
                }}
                onDragLeave={() => setDragOver(false)}
                onDrop={handleDrop}
                onClick={() => fileInputRef.current?.click()}
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  accept={getAcceptString()}
                  onChange={handleInputChange}
                  className="hidden"
                />

                {preview && file?.type.startsWith('image/') ? (
                  <div className="space-y-3">
                    <img
                      src={preview}
                      alt="Preview"
                      className="max-h-40 mx-auto rounded-lg object-contain"
                    />
                    <p className="text-sm text-gray-600">{file?.name}</p>
                    <p className="text-xs text-gray-400">{file && formatFileSize(file.size)}</p>
                  </div>
                ) : file ? (
                  <div className="space-y-3">
                    <div className="flex justify-center">{getFileIcon()}</div>
                    <p className="text-sm text-gray-600">{file.name}</p>
                    <p className="text-xs text-gray-400">{formatFileSize(file.size)}</p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    <div className="flex justify-center">
                      <Upload className="w-12 h-12 text-gray-400" />
                    </div>
                    <div>
                      <p className="text-gray-600">
                        Drag and drop or <span className="text-blue-600 font-medium">browse</span>
                      </p>
                      <p className="text-xs text-gray-400 mt-1">
                        Max size: {getDisplayMaxSize()}
                      </p>
                    </div>
                  </div>
                )}
              </div>

              {/* Change file button */}
              {file && !uploading && (
                <button
                  onClick={() => {
                    setFile(null);
                    setPreview(null);
                  }}
                  className="text-sm text-gray-500 hover:text-gray-700"
                >
                  Choose different file
                </button>
              )}
            </>
          ) : (
            <>
              {/* External URL input */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">URL</label>
                <input
                  type="url"
                  value={externalUrl}
                  onChange={(e) => setExternalUrl(e.target.value)}
                  placeholder="https://youtube.com/watch?v=..."
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
                <p className="text-xs text-gray-400 mt-1">
                  Supports YouTube, Vimeo, and other URLs
                </p>
              </div>

              {/* Title input */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Title (optional)</label>
                <input
                  type="text"
                  value={linkTitle}
                  onChange={(e) => setLinkTitle(e.target.value)}
                  placeholder="Enter a title for this link"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* YouTube Thumbnail Preview */}
              {linkThumbnail && (
                <div className="border rounded-lg p-3 bg-gray-50">
                  <p className="text-xs text-gray-500 mb-2">Video Thumbnail Preview</p>
                  <img
                    src={linkThumbnail}
                    alt="Video thumbnail"
                    className="w-full rounded-lg"
                  />
                </div>
              )}
            </>
          )}

          {/* Error message */}
          {error && (
            <div className="p-3 bg-red-50 text-red-700 text-sm rounded-lg">
              {error}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-3 p-4 border-t bg-gray-50 rounded-b-lg">
          <button
            onClick={handleClose}
            disabled={uploading}
            className="px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            onClick={handleUpload}
            disabled={(uploadMode === 'file' && !file) || (uploadMode === 'link' && !externalUrl) || uploading || loadingFileTypes}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {uploading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                {uploadMode === 'file' ? 'Uploading...' : 'Saving...'}
              </>
            ) : (
              <>
                {uploadMode === 'file' ? <Upload className="w-4 h-4" /> : <Link2 className="w-4 h-4" />}
                {uploadMode === 'file' ? 'Upload' : 'Save Link'}
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
