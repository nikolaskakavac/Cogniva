interface ConfirmDialogProps {
  open: boolean
  title: string
  description: string
  confirming?: boolean
  onCancel: () => void
  onConfirm: () => void
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirming = false,
  onCancel,
  onConfirm,
}: ConfirmDialogProps) {
  if (!open) return null

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={onCancel}>
      <section
        aria-modal="true"
        className="modal"
        role="dialog"
        aria-labelledby="confirm-dialog-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <h2 id="confirm-dialog-title">{title}</h2>
        <p>{description}</p>
        <div className="modal-actions">
          <button className="button button-secondary" type="button" onClick={onCancel} disabled={confirming}>
            Otkaži
          </button>
          <button className="button button-danger" type="button" onClick={onConfirm} disabled={confirming}>
            {confirming ? 'Brisanje…' : 'Obriši'}
          </button>
        </div>
      </section>
    </div>
  )
}
