const API_URL = 'http://localhost:5089/api';

let currentUser = null;
let token = localStorage.getItem('sigecam_token');
let centrosCache = [];

// Utilidad para mostrar mensajes
function showAlert(message, type = 'success') {
    const box = document.getElementById('alert-box');
    box.textContent = message;
    box.className = `alert ${type}`;
    box.classList.remove('hidden');
    setTimeout(() => box.classList.add('hidden'), 5000);
}

// Utilidad para peticiones HTTP con JWT
async function apiFetch(endpoint, options = {}) {
    const headers = {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...options.headers
    };

    const response = await fetch(`${API_URL}${endpoint}`, {
        ...options,
        headers
    });

    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
        throw new Error(data.mensaje || data.title || 'Error en la operación');
    }
    return data;
}

// Decodificar payload del JWT para obtener id, nombre y rol
function parseJwt(jwt) {
    try {
        const base64Url = jwt.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(atob(base64));
    } catch {
        return null;
    }
}

function initSession() {
    if (!token) {
        showAuthView();
        return;
    }

    const payload = parseJwt(token);
    if (!payload) {
        logout();
        return;
    }

    currentUser = {
        id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || payload.nameid,
        nombre: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || payload.unique_name || 'Usuario',
        rol: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role
    };

    document.getElementById('auth-section').classList.add('hidden');
    document.getElementById('user-bar').classList.remove('hidden');
    document.getElementById('user-info').textContent = `${currentUser.nombre} (${currentUser.rol})`;

    document.getElementById('panel-paciente').classList.add('hidden');
    document.getElementById('panel-medico').classList.add('hidden');
    document.getElementById('panel-admin').classList.add('hidden');

    if (currentUser.rol === 'Paciente') {
        document.getElementById('panel-paciente').classList.remove('hidden');
        loadPacienteData();
    } else if (currentUser.rol === 'PersonalSalud') {
        document.getElementById('panel-medico').classList.remove('hidden');
        loadMedicoData();
    } else if (currentUser.rol === 'Admin') {
        document.getElementById('panel-admin').classList.remove('hidden');
        loadAdminData();
    }
}

function showAuthView() {
    document.getElementById('auth-section').classList.remove('hidden');
    document.getElementById('user-bar').classList.add('hidden');
    document.getElementById('panel-paciente').classList.add('hidden');
    document.getElementById('panel-medico').classList.add('hidden');
    document.getElementById('panel-admin').classList.add('hidden');
}

function logout() {
    localStorage.removeItem('sigecam_token');
    token = null;
    currentUser = null;
    showAuthView();
}

document.getElementById('btn-logout').addEventListener('click', logout);

// --- AUTENTICACIÓN ---
document.getElementById('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const email = document.getElementById('login-email').value;
        const password = document.getElementById('login-password').value;
        const res = await apiFetch('/Auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });
        token = res.token;
        localStorage.setItem('sigecam_token', token);
        showAlert('Sesión iniciada correctamente.');
        initSession();
    } catch (err) {
        showAlert(err.message, 'error');
    }
});

document.getElementById('register-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const nombreCompleto = document.getElementById('reg-nombre').value;
        const email = document.getElementById('reg-email').value;
        const password = document.getElementById('reg-password').value;
        await apiFetch('/Auth/register', {
            method: 'POST',
            body: JSON.stringify({ nombreCompleto, email, password, rol: 'Paciente' })
        });
        showAlert('Cuenta creada con éxito. Ahora inicia sesión.');
        e.target.reset();
    } catch (err) {
        showAlert(err.message, 'error');
    }
});

// --- CARGA DE CENTROS ---
async function fetchCentros() {
    centrosCache = await apiFetch('/HealthCenters');
    return centrosCache;
}

function fillCentroSelect(selectId) {
    const select = document.getElementById(selectId);
    if (!select) return;
    select.innerHTML = centrosCache
        .map((c) => `<option value="${c.id}">${c.nombre} - ${c.ubicacion}</option>`)
        .join('');
}

// --- PANEL PACIENTE ---
async function loadPacienteData() {
    await fetchCentros();
    fillCentroSelect('cita-centro');
    await loadMisCitas();
    await loadMisDispensaciones();
}

async function loadMisCitas() {
    const container = document.getElementById('mis-citas-list');
    try {
        const citas = await apiFetch('/Appointments/my');
        if (!citas.length) {
            container.innerHTML = '<p>No tienes citas registradas.</p>';
            return;
        }
        container.innerHTML = citas
            .map((c) => {
                const badgeClass =
                    c.estado === 'programada' ? 'badge-ok' : c.estado === 'atendida' ? 'badge-warn' : 'badge-cancel';
                return `
          <div class="item-box">
            <strong>${c.servicio}</strong> <span class="badge ${badgeClass}">${c.estado.toUpperCase()}</span>
            <p>Fecha: ${c.fecha} | ID Cita: <code>${c.id}</code></p>
            ${
                    c.estado === 'programada'
                        ? `<button class="btn btn-danger" onclick="cancelarCita('${c.id}')">Cancelar Cita</button>`
                        : ''
                }
          </div>`;
            })
            .join('');
    } catch (err) {
        container.innerHTML = `<p>${err.message}</p>`;
    }
}

async function loadMisDispensaciones() {
    const container = document.getElementById('mis-dispensaciones-list');
    try {
        const list = await apiFetch(`/Consultations/patient/${currentUser.id}/dispensations`);
        if (!list.length) {
            container.innerHTML = '<p>Aún no has recibido medicamentos.</p>';
            return;
        }
        container.innerHTML = list
            .map(
                (d) => `
        <div class="item-box">
          <strong>${d.medicamentoNombre}</strong> (Cantidad: ${d.cantidadEntregada})
          <p>Fecha: ${new Date(d.fechaDispensacion).toLocaleString()}</p>
        </div>`
            )
            .join('');
    } catch (err) {
        container.innerHTML = `<p>${err.message}</p>`;
    }
}

window.cancelarCita = async (citaId) => {
    try {
        await apiFetch(`/Appointments/${citaId}/cancel`, { method: 'PATCH' });
        showAlert('Cita cancelada y cupo liberado.');
        loadMisCitas();
    } catch (err) {
        showAlert(err.message, 'error');
    }
};

document.getElementById('btn-refresh-citas').addEventListener('click', loadMisCitas);

document.getElementById('cita-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const centroId = document.getElementById('cita-centro').value;
        const servicio = document.getElementById('cita-servicio').value;
        const fecha = document.getElementById('cita-fecha').value;
        await apiFetch('/Appointments', {
            method: 'POST',
            body: JSON.stringify({ centroId, servicio, fecha })
        });
        showAlert('Cita reservada con éxito.');
        loadMisCitas();
    } catch (err) {
        showAlert(err.message, 'error');
    }
});

// --- PANEL MÉDICO ---
async function loadMedicoData() {
    await fetchCentros();
    fillCentroSelect('cons-centro');
    if (centrosCache.length) {
        await loadMedicamentosSelect(centrosCache[0].id);
    }
}

document.getElementById('cons-centro').addEventListener('change', (e) => {
    loadMedicamentosSelect(e.target.value);
});

async function loadMedicamentosSelect(centroId) {
    const select = document.getElementById('cons-medicamento');
    try {
        const meds = await apiFetch(`/Medications/center/${centroId}`);
        select.innerHTML =
            '<option value="">-- Ninguno --</option>' +
            meds
                .map((m) => `<option value="${m.id}">${m.nombre} (${m.presentacion}) - Stock: ${m.stockActual}</option>`)
                .join('');
    } catch {
        select.innerHTML = '<option value="">-- Sin medicamentos --</option>';
    }
}

document.getElementById('consulta-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const citaId = document.getElementById('cons-cita-id').value.trim();
        const diagnostico = document.getElementById('cons-diagnostico').value;
        const observaciones = document.getElementById('cons-observaciones').value;
        const medId = document.getElementById('cons-medicamento').value;
        const cantidad = parseInt(document.getElementById('cons-cantidad').value, 10);

        const medicamentosDispensados = medId ? [{ medicamentoId: medId, cantidad }] : [];

        await apiFetch('/Consultations', {
            method: 'POST',
            body: JSON.stringify({ citaId, diagnostico, observaciones, medicamentosDispensados })
        });

        showAlert('Consulta registrada y medicamentos dispensados.');
        e.target.reset();
        loadMedicamentosSelect(document.getElementById('cons-centro').value);
    } catch (err) {
        showAlert(err.message, 'error');
    }
});

// --- PANEL ADMINISTRADOR ---
async function loadAdminData() {
    await fetchCentros();
    fillCentroSelect('cupo-centro');
    fillCentroSelect('med-centro');
    fillCentroSelect('inv-centro-filter');
    if (centrosCache.length) {
        await loadInventario(centrosCache[0].id);
    }
}

document.getElementById('inv-centro-filter').addEventListener('change', (e) => {
    loadInventario(e.target.value);
});

async function loadInventario(centroId) {
    const container = document.getElementById('inventario-list');
    try {
        const meds = await apiFetch(`/Medications/center/${centroId}`);
        if (!meds.length) {
            container.innerHTML = '<p>No hay medicamentos registrados en este centro.</p>';
            return;
        }
        container.innerHTML = meds
            .map(
                (m) => `
        <div class="item-box">
          <strong>${m.nombre}</strong> (${m.presentacion})
          ${m.alertaStockBajo ? '<span class="badge badge-cancel">STOCK BAJO</span>' : '<span class="badge badge-ok">OK</span>'}
          <p>Stock actual: <strong>${m.stockActual}</strong> (Mínimo: ${m.stockMinimo})</p>
        </div>`
            )
            .join('');
    } catch (err) {
        container.innerHTML = `<p>${err.message}</p>`;
    }
}

document.getElementById('centro-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const nombre = document.getElementById('centro-nombre').value;
        const ubicacion = document.getElementById('centro-ubicacion').value;
        const servicios = document
            .getElementById('centro-servicios')
            .value.split(',')
            .map((s) => s.trim())
            .filter(Boolean);

        await apiFetch('/HealthCenters', {
            method: 'POST',
            body: JSON.stringify({ nombre, ubicacion, servicios })
        });
        showAlert('Centro de salud registrado.');
        e.target.reset();
        loadAdminData();
    } catch (err) {
        showAlert(err.message, 'error');
    }
});

document.getElementById('cupo-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const centroId = document.getElementById('cupo-centro').value;
        const servicio = document.getElementById('cupo-servicio').value;
        const fecha = document.getElementById('cupo-fecha').value;
        const cuposMaximos = parseInt(document.getElementById('cupo-maximo').value, 10);

        await apiFetch('/Appointments/slots', {
            method: 'POST',
            body: JSON.stringify({ centroId, servicio, fecha, cuposMaximos })
        });
        showAlert('Cupos diarios configurados.');
    } catch (err) {
        showAlert(err.message, 'error');
    }
});

document.getElementById('med-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const centroId = document.getElementById('med-centro').value;
        const nombre = document.getElementById('med-nombre').value;
        const presentacion = document.getElementById('med-presentacion').value;
        const stockActual = parseInt(document.getElementById('med-stock').value, 10);
        const stockMinimo = parseInt(document.getElementById('med-minimo').value, 10);

        await apiFetch('/Medications', {
            method: 'POST',
            body: JSON.stringify({ centroId, nombre, presentacion, stockActual, stockMinimo })
        });
        showAlert('Medicamento agregado al inventario.');
        e.target.reset();
        loadInventario(centroId);
    } catch (err) {
        showAlert(err.message, 'error');
    }
});

// Arrancar aplicación
initSession();