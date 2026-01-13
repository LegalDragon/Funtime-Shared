import React from 'react';

export interface SkillBadgeProps {
  level: number;
  size?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
  className?: string;
}

export const SkillBadge: React.FC<SkillBadgeProps> = ({
  level,
  size = 'md',
  showLabel = true,
  className = '',
}) => {
  const getColor = (level: number): string => {
    if (level < 2.5) return 'bg-gray-500';
    if (level < 3.0) return 'bg-green-500';
    if (level < 3.5) return 'bg-blue-500';
    if (level < 4.0) return 'bg-purple-500';
    if (level < 4.5) return 'bg-orange-500';
    return 'bg-red-500';
  };

  const getLabel = (level: number): string => {
    if (level < 2.0) return 'Beginner';
    if (level < 2.5) return 'Novice';
    if (level < 3.0) return 'Intermediate';
    if (level < 3.5) return 'Advanced';
    if (level < 4.0) return 'Skilled';
    if (level < 4.5) return 'Expert';
    if (level < 5.0) return 'Pro';
    return 'Elite';
  };

  const sizeStyles = {
    sm: 'text-xs px-2 py-0.5',
    md: 'text-sm px-3 py-1',
    lg: 'text-base px-4 py-1.5',
  };

  return (
    <span
      className={`inline-flex items-center rounded-full text-white font-medium ${getColor(level)} ${sizeStyles[size]} ${className}`}
    >
      {level.toFixed(1)}
      {showLabel && <span className="ml-1">({getLabel(level)})</span>}
    </span>
  );
};
