import type { Meta, StoryObj } from '@storybook/react';
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from './Card';
import { Button } from './Button';

const meta: Meta<typeof Card> = {
  title: 'Components/Card',
  component: Card,
  tags: ['autodocs'],
  argTypes: {
    padding: {
      control: 'select',
      options: ['none', 'sm', 'md', 'lg'],
    },
  },
};

export default meta;
type Story = StoryObj<typeof Card>;

export const Default: Story = {
  args: {
    children: 'This is a basic card with some content.',
  },
};

export const WithAllParts: Story = {
  render: () => (
    <Card>
      <CardHeader>
        <CardTitle>Card Title</CardTitle>
      </CardHeader>
      <CardContent>
        <p>This is the main content area of the card. You can put any content here.</p>
      </CardContent>
      <CardFooter>
        <Button size="sm">Action</Button>
      </CardFooter>
    </Card>
  ),
};

export const SmallPadding: Story = {
  args: {
    padding: 'sm',
    children: 'Card with small padding',
  },
};

export const LargePadding: Story = {
  args: {
    padding: 'lg',
    children: 'Card with large padding',
  },
};

export const NoPadding: Story = {
  args: {
    padding: 'none',
    children: (
      <div style={{ padding: '1rem', background: '#f0f0f0' }}>
        Card with no padding - content provides its own
      </div>
    ),
  },
};

export const UserProfileCard: Story = {
  render: () => (
    <Card style={{ maxWidth: '300px' }}>
      <CardHeader>
        <CardTitle>Player Profile</CardTitle>
      </CardHeader>
      <CardContent>
        <div style={{ textAlign: 'center' }}>
          <div
            style={{
              width: 80,
              height: 80,
              borderRadius: '50%',
              background: '#10b981',
              margin: '0 auto 1rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'white',
              fontSize: '2rem',
            }}
          >
            JD
          </div>
          <h4 style={{ margin: 0 }}>John Doe</h4>
          <p style={{ color: '#666', margin: '0.5rem 0' }}>Skill Level: 4.0</p>
          <p style={{ color: '#666', margin: 0 }}>Seattle, WA</p>
        </div>
      </CardContent>
      <CardFooter>
        <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center' }}>
          <Button size="sm" variant="outline">
            Message
          </Button>
          <Button size="sm">Follow</Button>
        </div>
      </CardFooter>
    </Card>
  ),
};

export const CardGrid: Story = {
  render: () => (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem' }}>
      <Card>
        <CardTitle>Community</CardTitle>
        <CardContent>Connect with local players</CardContent>
      </Card>
      <Card>
        <CardTitle>College</CardTitle>
        <CardContent>Campus pickleball clubs</CardContent>
      </Card>
      <Card>
        <CardTitle>Date</CardTitle>
        <CardContent>Find your match on the court</CardContent>
      </Card>
    </div>
  ),
};
