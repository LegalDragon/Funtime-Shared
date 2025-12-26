import type { Meta, StoryObj } from '@storybook/react';
import { SiteBadge, SiteList } from './SiteBadge';

const meta: Meta<typeof SiteBadge> = {
  title: 'Components/SiteBadge',
  component: SiteBadge,
  tags: ['autodocs'],
  argTypes: {
    siteKey: {
      control: 'select',
      options: ['community', 'college', 'date', 'jobs'],
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
    },
  },
};

export default meta;
type Story = StoryObj<typeof SiteBadge>;

export const Community: Story = {
  args: {
    siteKey: 'community',
  },
};

export const College: Story = {
  args: {
    siteKey: 'college',
  },
};

export const Date: Story = {
  args: {
    siteKey: 'date',
  },
};

export const Jobs: Story = {
  args: {
    siteKey: 'jobs',
  },
};

export const AllSites: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
      <SiteBadge siteKey="community" />
      <SiteBadge siteKey="college" />
      <SiteBadge siteKey="date" />
      <SiteBadge siteKey="jobs" />
    </div>
  ),
};

export const AllSizes: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
      <SiteBadge siteKey="community" size="sm" />
      <SiteBadge siteKey="community" size="md" />
      <SiteBadge siteKey="community" size="lg" />
    </div>
  ),
};

export const SiteListExample: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      <div>
        <p style={{ marginBottom: '0.5rem' }}>User&apos;s sites:</p>
        <SiteList sites={['community', 'college']} />
      </div>
      <div>
        <p style={{ marginBottom: '0.5rem' }}>All sites:</p>
        <SiteList sites={['community', 'college', 'date', 'jobs']} />
      </div>
    </div>
  ),
};
