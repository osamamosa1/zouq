import type { ReactNode } from 'react';

export function PageShell({ title, children }: { title: string; children: ReactNode }) {
  return (
    <>
      <header className="main-header">
        <h2>{title}</h2>
      </header>
      <div className="main-content">{children}</div>
    </>
  );
}
