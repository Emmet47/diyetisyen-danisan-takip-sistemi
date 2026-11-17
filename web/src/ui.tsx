import { useEffect, useRef, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { ApiError } from './api/http'

export function errorText(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 403) return 'Bu işlem için yetkiniz yok.'
    if (error.status === 429) return 'Çok fazla deneme. Biraz sonra tekrar deneyin.'
    const fields = Object.values(error.errors ?? {}).flat()
    return [error.message, ...fields].filter(Boolean).join(' ') || 'İstek tamamlanamadı.'
  }
  if (error instanceof TypeError || error instanceof SyntaxError) return 'API’ye ulaşılamadı. Sunucuyu kontrol edip tekrar deneyin.'
  return error instanceof Error ? error.message : 'Beklenmeyen bir hata oluştu.'
}

export function useLoad<T>(load: () => Promise<T>, key: string) {
  const [result, setResult] = useState<{ key: string; data?: T; error?: string }>({ key: '' })
  const [version, setVersion] = useState(0)
  useEffect(() => {
    let current = true
    load().then(data => { if (current) setResult({ key, data }) }).catch(error => {
      if (current) setResult({ key, error: errorText(error) })
    })
    return () => { current = false }
    // The key describes the request, including its filters.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key, version])
  return { data: result.key === key ? result.data : undefined, error: result.key === key ? result.error : undefined, reload: () => setVersion(value => value + 1) }
}

export function useAction(onDone?: () => void) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const running = useRef(false)
  async function run(action: () => Promise<unknown> | unknown) {
    if (running.current) return
    running.current = true
    setError('')
    setBusy(true)
    try {
      await action()
      onDone?.()
    } catch (failure) {
      setError(errorText(failure))
    } finally {
      running.current = false
      setBusy(false)
    }
  }
  return { busy, error, run, clearError: () => setError('') }
}

export function values(event: FormEvent<HTMLFormElement>) {
  event.preventDefault()
  return Object.fromEntries(new FormData(event.currentTarget)) as Record<string, string>
}

export function Brand() {
  return <div className="brand"><span className="brand-mark" aria-hidden="true"><i /></span><span><b>DDTS</b><small>Diyetisyen takip sistemi</small></span></div>
}
export function Title({ title, subtitle, action }: { title: string; subtitle?: string; action?: ReactNode }) {
  return <header className="page-title"><div><h1>{title}</h1>{subtitle && <p>{subtitle}</p>}</div>{action}</header>
}
export function Badge({ children, tone = 'green' }: { children: ReactNode; tone?: 'green' | 'amber' | 'gray' | 'red' }) {
  return <span className={'badge ' + tone}>{children}</span>
}
export function Message({ children, type = 'error' }: { children?: ReactNode; type?: 'error' | 'info' | 'success' }) {
  return children ? <div className={'message ' + type} role={type === 'error' ? 'alert' : 'status'}>{children}</div> : null
}
export function Wait({ error, reload }: { error?: string; reload?: () => void }) {
  return error ? <div className="panel empty"><Message>{error}</Message>{reload && <button className="button secondary" onClick={reload}>Tekrar dene</button>}</div> : <p className="panel empty" role="status">Yükleniyor…</p>
}
export function Empty({ children }: { children: ReactNode }) { return <p className="empty">{children}</p> }
export function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="field"><span>{label}</span>{children}</label>
}
export function Modal({ title, close, children }: { title: string; close: () => void; children: ReactNode }) {
  const dialog = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const element = dialog.current
    element?.showModal()
    return () => { element?.close() }
  }, [])
  return <dialog ref={dialog} aria-label={title} onCancel={event => { event.preventDefault(); close() }}>
    <header className="modal-title"><h2>{title}</h2><button type="button" className="icon-button" aria-label="Kapat" onClick={close}>×</button></header>
    {children}
  </dialog>
}
export function Table({ heads, children }: { heads: string[]; children: ReactNode }) {
  return <div className="table-wrap"><table><thead><tr>{heads.map((head, i) => <th key={i} scope="col">{head || <span className="sr-only">İşlemler</span>}</th>)}</tr></thead><tbody>{children}</tbody></table></div>
}
export function Pager({ page, pageSize, totalCount, change }: { page: number; pageSize: number; totalCount: number; change: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize))
  return pages > 1 ? <div className="pager"><button className="button secondary" disabled={page === 1} onClick={() => change(page - 1)}>Önceki</button><span>{page} / {pages}</span><button className="button secondary" disabled={page >= pages} onClick={() => change(page + 1)}>Sonraki</button></div> : null
}
