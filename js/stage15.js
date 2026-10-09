// Stage 15: progressive enhancement for authenticated inventory export.
(() => {
  const ready = () => {
    const anchor = document.querySelector('[data-stage15-export]');
    if (!anchor) return;
    anchor.addEventListener('click', async () => {
      anchor.disabled = true;
      try {
        const response = await fetch('/api/admin/inventory-export', { credentials: 'same-origin' });
        if (!response.ok) throw new Error('Export unavailable (' + response.status + ')');
        const url = URL.createObjectURL(await response.blob());
        const link = document.createElement('a'); link.href = url; link.download = 'dinesphere-inventory.csv';
        document.body.appendChild(link); link.click(); link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
      } catch (error) { alert(error.message); } finally { anchor.disabled = false; }
    });
  };
  document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', ready) : ready();
})();
