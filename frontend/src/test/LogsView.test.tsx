import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { LogsView } from '../components/Views/LogsView';
import { useLogStore, appLog } from '../store/logStore';

describe('LogsView', () => {
  beforeEach(() => {
    useLogStore.getState().clear();
  });

  it('should render empty state', () => {
    render(<LogsView />);
    expect(screen.getByText('Logs en Tiempo Real')).toBeInTheDocument();
    expect(screen.getByText(/sin logs todavía/i)).toBeInTheDocument();
  });

  it('should display log entries', () => {
    appLog.info('App', 'Dashboard iniciado');
    appLog.error('LLM', 'Timeout al conectar');
    render(<LogsView />);
    expect(screen.getByText('Dashboard iniciado')).toBeInTheDocument();
    expect(screen.getByText('Timeout al conectar')).toBeInTheDocument();
  });

  it('should show correct entry count', () => {
    appLog.info('App', 'msg1');
    appLog.warn('API', 'msg2');
    appLog.error('LLM', 'msg3');
    render(<LogsView />);
    expect(screen.getByText('3 entradas')).toBeInTheDocument();
    expect(screen.getByText('1 errores')).toBeInTheDocument();
    expect(screen.getByText('1 warnings')).toBeInTheDocument();
  });

  it('should filter by search text', () => {
    appLog.info('App', 'alpha message');
    appLog.info('App', 'beta message');
    render(<LogsView />);

    const search = screen.getByPlaceholderText('Buscar en logs...');
    fireEvent.change(search, { target: { value: 'alpha' } });

    expect(screen.getByText('alpha message')).toBeInTheDocument();
    expect(screen.queryByText('beta message')).not.toBeInTheDocument();
  });

  it('should clear logs on button click', () => {
    appLog.info('App', 'will disappear');
    render(<LogsView />);
    expect(screen.getByText('will disappear')).toBeInTheDocument();

    fireEvent.click(screen.getByTitle('Limpiar logs'));
    expect(screen.queryByText('will disappear')).not.toBeInTheDocument();
  });

  it('should show pause indicator', () => {
    appLog.info('App', 'test');
    render(<LogsView />);
    fireEvent.click(screen.getByTitle('Pausar'));
    expect(screen.getByText(/logs pausados/i)).toBeInTheDocument();
  });
});
