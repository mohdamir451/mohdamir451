(() => {
  const sidebar = document.getElementById('sidebar');
  const sidebarToggle = document.getElementById('sidebarToggle');

  const setModalVisibility = (id, isVisible) => {
    if (!id) return;
    const modal = document.getElementById(id);
    if (!modal) return;
    modal.hidden = !isVisible;
  };

  if (sidebarToggle && sidebar) {
    sidebarToggle.addEventListener('click', () => {
      sidebar.classList.toggle('collapsed');
    });
  }

  document.addEventListener('click', (event) => {
    const openTrigger = event.target.closest('[data-open-modal]');
    if (openTrigger) {
      event.preventDefault();
      setModalVisibility(openTrigger.getAttribute('data-open-modal'), true);
      return;
    }

    const closeTrigger = event.target.closest('[data-close-modal]');
    if (closeTrigger) {
      event.preventDefault();
      setModalVisibility(closeTrigger.getAttribute('data-close-modal'), false);
      return;
    }

    const backdrop = event.target.closest('.modal-backdrop');
    if (backdrop && event.target === backdrop) {
      backdrop.hidden = true;
    }
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      document.querySelectorAll('.modal-backdrop:not([hidden])').forEach((modal) => {
        modal.hidden = true;
      });
    }
  });
})();
