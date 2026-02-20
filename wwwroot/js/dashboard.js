(() => {
  const sidebar = document.getElementById('sidebar');
  const sidebarToggle = document.getElementById('sidebarToggle');
  const modal = document.getElementById('inviteModal');

  if (sidebarToggle && sidebar) {
    sidebarToggle.addEventListener('click', () => {
      sidebar.classList.toggle('collapsed');
    });
  }

  document.querySelectorAll('[data-open-modal]').forEach(button => {
    button.addEventListener('click', () => {
      const target = button.getAttribute('data-open-modal');
      const element = target ? document.getElementById(target) : null;
      if (element) {
        element.hidden = false;
      }
    });
  });

  document.querySelectorAll('[data-close-modal]').forEach(button => {
    button.addEventListener('click', () => {
      const target = button.getAttribute('data-close-modal');
      const element = target ? document.getElementById(target) : null;
      if (element) {
        element.hidden = true;
      }
    });
  });

  if (modal) {
    modal.addEventListener('click', (event) => {
      if (event.target === modal) {
        modal.hidden = true;
      }
    });
  }
})();
