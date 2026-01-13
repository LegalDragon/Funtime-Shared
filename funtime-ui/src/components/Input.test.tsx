import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Input } from './Input';

describe('Input', () => {
  it('renders input element', () => {
    render(<Input />);
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('renders with label', () => {
    render(<Input label="Email" />);
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
  });

  it('handles value changes', async () => {
    const handleChange = vi.fn();
    const user = userEvent.setup();

    render(<Input onChange={handleChange} />);
    await user.type(screen.getByRole('textbox'), 'test');

    expect(handleChange).toHaveBeenCalled();
  });

  it('displays error message', () => {
    render(<Input error="This field is required" />);
    expect(screen.getByText('This field is required')).toBeInTheDocument();
  });

  it('applies error styles when error is present', () => {
    render(<Input error="Error" />);
    expect(screen.getByRole('textbox')).toHaveClass('border-red-500');
  });

  it('displays helper text', () => {
    render(<Input helperText="Enter your email address" />);
    expect(screen.getByText('Enter your email address')).toBeInTheDocument();
  });

  it('hides helper text when error is present', () => {
    render(<Input helperText="Helper text" error="Error message" />);
    expect(screen.queryByText('Helper text')).not.toBeInTheDocument();
    expect(screen.getByText('Error message')).toBeInTheDocument();
  });

  it('accepts placeholder', () => {
    render(<Input placeholder="Enter text..." />);
    expect(screen.getByPlaceholderText('Enter text...')).toBeInTheDocument();
  });

  it('can be disabled', () => {
    render(<Input disabled />);
    expect(screen.getByRole('textbox')).toBeDisabled();
  });

  it('accepts custom className', () => {
    render(<Input className="custom-class" />);
    expect(screen.getByRole('textbox')).toHaveClass('custom-class');
  });

  it('forwards ref correctly', () => {
    const ref = vi.fn();
    render(<Input ref={ref} />);
    expect(ref).toHaveBeenCalled();
  });

  it('generates unique id when not provided', () => {
    render(<Input label="Field 1" />);
    const input = screen.getByLabelText('Field 1');
    expect(input).toHaveAttribute('id');
  });

  it('uses provided id', () => {
    render(<Input id="my-input" label="My Input" />);
    expect(screen.getByLabelText('My Input')).toHaveAttribute('id', 'my-input');
  });
});
