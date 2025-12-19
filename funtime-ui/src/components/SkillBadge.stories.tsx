import type { Meta, StoryObj } from '@storybook/react';
import { SkillBadge } from './SkillBadge';

const meta: Meta<typeof SkillBadge> = {
  title: 'Components/SkillBadge',
  component: SkillBadge,
  tags: ['autodocs'],
  argTypes: {
    level: {
      control: { type: 'range', min: 1.0, max: 5.5, step: 0.5 },
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
    },
    showLabel: { control: 'boolean' },
  },
};

export default meta;
type Story = StoryObj<typeof SkillBadge>;

export const Beginner: Story = {
  args: {
    level: 1.5,
  },
};

export const Intermediate: Story = {
  args: {
    level: 3.0,
  },
};

export const Advanced: Story = {
  args: {
    level: 4.0,
  },
};

export const Pro: Story = {
  args: {
    level: 4.5,
  },
};

export const Elite: Story = {
  args: {
    level: 5.5,
  },
};

export const WithoutLabel: Story = {
  args: {
    level: 3.5,
    showLabel: false,
  },
};

export const AllLevels: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
      <SkillBadge level={1.5} />
      <SkillBadge level={2.0} />
      <SkillBadge level={2.5} />
      <SkillBadge level={3.0} />
      <SkillBadge level={3.5} />
      <SkillBadge level={4.0} />
      <SkillBadge level={4.5} />
      <SkillBadge level={5.0} />
      <SkillBadge level={5.5} />
    </div>
  ),
};

export const AllSizes: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
      <SkillBadge level={3.5} size="sm" />
      <SkillBadge level={3.5} size="md" />
      <SkillBadge level={3.5} size="lg" />
    </div>
  ),
};
