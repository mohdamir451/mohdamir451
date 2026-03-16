(() => {
  const state = {
    token: null,
    pdfValues: [],
    fields: [],
    auditTrail: [],
    undoStack: [],
    zoom: 100,
    page: 1,
    totalPages: 1
  };

  const qs = (id) => document.getElementById(id);
  const statusMessage = qs('statusMessage');
  const tableBody = qs('comparisonTable')?.querySelector('tbody');

  const setStatus = (msg) => {
    if (statusMessage) statusMessage.textContent = msg;
  };

  const renderTable = () => {
    if (!tableBody) return;
    tableBody.innerHTML = '';

    state.fields.forEach((field) => {
      const row = document.createElement('tr');
      row.className = field.isMatch ? 'match-row' : 'mismatch-row';

      row.innerHTML = `
        <td>${field.label}</td>
        <td>${field.apiValue ?? ''}</td>
        <td>${field.pdfValue ?? ''}</td>
        <td><input aria-label="Correct value for ${field.label}" data-key="${field.key}" class="correct-input" value="${field.correctedValue ?? ''}" /></td>
        <td>${field.confidenceScore}</td>
        <td><span class="reason-code" title="${field.reasonDescription}">${field.reasonCode}</span></td>
      `;

      tableBody.appendChild(row);
    });

    document.querySelectorAll('.correct-input').forEach((input) => {
      input.addEventListener('change', (e) => {
        const key = e.target.getAttribute('data-key');
        const field = state.fields.find((x) => x.key === key);
        if (!field) return;
        const oldValue = field.correctedValue;
        const newValue = e.target.value;
        field.correctedValue = newValue;

        state.undoStack.push({ key, oldValue, newValue });
        state.auditTrail.push({
          fieldKey: key,
          oldValue,
          newValue,
          changedBy: 'CurrentUser',
          changedAtUtc: new Date().toISOString()
        });

        setStatus(`Updated ${field.label}. Change recorded in audit trail.`);
      });
    });
  };

  const updateViewerUi = () => {
    qs('zoomLabel').textContent = `Zoom: ${state.zoom}%`;
    qs('pageStatus').textContent = `Page: ${state.page} / ${state.totalPages}`;
    const frame = qs('pdfViewerFrame');
    frame.style.transform = `scale(${state.zoom / 100})`;
    frame.style.transformOrigin = 'top left';
  };

  qs('uploadBtn')?.addEventListener('click', async () => {
    const file = qs('pdfUpload')?.files?.[0];
    if (!file) {
      setStatus('Please select a PDF before uploading.');
      return;
    }

    const formData = new FormData();
    formData.append('file', file);

    const res = await fetch('/api/comparison/upload-pdf', { method: 'POST', body: formData });
    if (!res.ok) {
      setStatus('Upload failed. Please check your PDF and try again.');
      return;
    }

    const data = await res.json();
    state.token = data.fileToken;
    state.pdfValues = data.pdfValues;

    qs('pdfViewerFrame').src = data.fileUrl;
    setStatus(`Uploaded ${data.fileName}. Ready for comparison.`);
  });

  qs('compareBtn')?.addEventListener('click', async () => {
    if (!state.pdfValues.length) {
      setStatus('Upload a PDF first to compare values.');
      return;
    }

    let apiValues;
    try {
      apiValues = JSON.parse(qs('apiJsonInput').value);
    } catch {
      setStatus('Invalid API JSON.');
      return;
    }

    const res = await fetch('/api/comparison/compare', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ apiValues, pdfValues: state.pdfValues })
    });

    if (!res.ok) {
      setStatus('Comparison failed.');
      return;
    }

    const result = await res.json();
    state.fields = result.fields;
    renderTable();

    setStatus(`Compared ${result.summary.totalFields} fields. Mismatches: ${result.summary.mismatchCount}.`);
  });

  qs('undoBtn')?.addEventListener('click', () => {
    const last = state.undoStack.pop();
    if (!last) {
      setStatus('No corrections to undo.');
      return;
    }

    const field = state.fields.find((x) => x.key === last.key);
    if (field) field.correctedValue = last.oldValue;
    renderTable();
    setStatus('Last correction undone.');
  });

  qs('submitBtn')?.addEventListener('click', async () => {
    const payload = {
      fileToken: state.token,
      validatedBy: 'CurrentUser',
      fields: state.fields,
      auditTrail: state.auditTrail
    };

    const res = await fetch('/api/comparison/submit', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!res.ok) {
      setStatus('Submit failed.');
      return;
    }

    setStatus('Validated data submitted successfully.');
  });

  qs('downloadBtn')?.addEventListener('click', async () => {
    const payload = {
      fileToken: state.token,
      validatedBy: 'CurrentUser',
      fields: state.fields,
      auditTrail: state.auditTrail
    };

    const res = await fetch('/api/comparison/export-excel', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!res.ok) {
      setStatus('Download failed.');
      return;
    }

    const blob = await res.blob();
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'validated-comparison-export.csv';
    link.click();
    URL.revokeObjectURL(link.href);
    setStatus('Excel-compatible file downloaded.');
  });

  qs('zoomInBtn')?.addEventListener('click', () => {
    state.zoom = Math.min(200, state.zoom + 10);
    updateViewerUi();
  });

  qs('zoomOutBtn')?.addEventListener('click', () => {
    state.zoom = Math.max(50, state.zoom - 10);
    updateViewerUi();
  });

  qs('nextPageBtn')?.addEventListener('click', () => {
    state.page = Math.min(state.totalPages, state.page + 1);
    updateViewerUi();
  });

  qs('prevPageBtn')?.addEventListener('click', () => {
    state.page = Math.max(1, state.page - 1);
    updateViewerUi();
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === '+') qs('zoomInBtn')?.click();
    if (e.key === '-') qs('zoomOutBtn')?.click();
    if (e.key === 'ArrowRight') qs('nextPageBtn')?.click();
    if (e.key === 'ArrowLeft') qs('prevPageBtn')?.click();
  });

  updateViewerUi();
})();
