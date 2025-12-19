import type { Meta, StoryObj } from '@storybook/react';
import { Avatar } from './Avatar';

const meta: Meta<typeof Avatar> = {
  title: 'Components/Avatar',
  component: Avatar,
  tags: ['autodocs'],
  argTypes: {
    size: {
      control: 'select',
      options: ['xs', 'sm', 'md', 'lg', 'xl'],
    },
  },
};

export default meta;
type Story = StoryObj<typeof Avatar>;

export const WithInitials: Story = {
  args: {
    name: 'John Doe',
    size: 'md',
  },
};

export const WithImage: Story = {
  args: {
    src: 'https://i.pravatar.cc/150?u=john',
    alt: 'John Doe',
    size: 'md',
  },
};

export const NoNameOrImage: Story = {
  args: {
    size: 'md',
  },
};

export const AllSizes: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
      <Avatar name="John Doe" size="xs" />
      <Avatar name="John Doe" size="sm" />
      <Avatar name="John Doe" size="md" />
      <Avatar name="John Doe" size="lg" />
      <Avatar name="John Doe" size="xl" />
    </div>
  ),
};

export const SingleName: Story = {
  args: {
    name: 'John',
    size: 'lg',
  },
};

export const MultipleAvatars: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '0.5rem' }}>
      <Avatar name="Alice Smith" />
      <Avatar name="Bob Johnson" />
      <Avatar name="Carol Williams" />
      <Avatar name="David Brown" />
    </div>
  ),
};
