// Configuration
const API_BASE_URL = window.location.origin;
const HEALTH_CHECK_INTERVAL = 5000; // 5 seconds
const API_INFO_INTERVAL = 10000; // 10 seconds

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    updateTime();
    fetchGatewayStatus();
    fetchApiInfo();

    setInterval(updateTime, 1000);
    setInterval(fetchGatewayStatus, HEALTH_CHECK_INTERVAL);
    setInterval(fetchApiInfo, API_INFO_INTERVAL);
});

// Update current time
function updateTime() {
    const now = new Date().toLocaleTimeString('de-DE');
    document.getElementById('current-time').textContent = now;
}

// Fetch gateway health status
async function fetchGatewayStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/health`);
        if (!response.ok) throw new Error('Health check failed');

        const data = await response.json();
        renderGatewayStatus(data);
        renderServices(data.services);
    } catch (error) {
        console.error('Error fetching gateway status:', error);
        showError('gateway-status', 'Failed to fetch gateway status');
    }
}

// Fetch API information
async function fetchApiInfo() {
    try {
        const response = await fetch(`${API_BASE_URL}/api-info`);
        if (!response.ok) throw new Error('API info failed');

        const data = await response.json();
        renderApiInfo(data);
    } catch (error) {
        console.error('Error fetching API info:', error);
    }
}

// Render gateway status
function renderGatewayStatus(status) {
    const container = document.getElementById('gateway-status');
    const statusClass = status.status.toLowerCase();

    container.innerHTML = `
        <div>
            <span class="status-badge ${statusClass}">${status.status}</span>
            <div class="status-details">
                <div class="status-detail">
                    <strong>Status</strong>
                    <span>${status.status}</span>
                </div>
                <div class="status-detail">
                    <strong>Uptime</strong>
                    <span>${formatUptime(status.uptimeSeconds)}</span>
                </div>
                <div class="status-detail">
                    <strong>Timestamp</strong>
                    <span>${new Date(status.timestamp).toLocaleString('de-DE')}</span>
                </div>
                <div class="status-detail">
                    <strong>Services</strong>
                    <span>${Object.keys(status.services).length}</span>
                </div>
            </div>
        </div>
    `;

    container.className = `status-card ${statusClass}`;
}

// Render services
function renderServices(services) {
    const container = document.getElementById('services-container');

    if (!services || Object.keys(services).length === 0) {
        container.innerHTML = '<p>No services configured</p>';
        return;
    }

    container.innerHTML = Object.entries(services).map(([name, service]) => {
        const statusClass = service.isHealthy ? 'healthy' : 'unhealthy';
        const statusText = service.isHealthy ? '✓ Healthy' : '✗ Unhealthy';

        return `
            <div class="service-card ${statusClass}">
                <div class="service-name">${name}</div>
                <div class="service-url">${service.url}</div>
                <div class="service-info">
                    <span class="service-status ${statusClass}">${statusText}</span>
                    <span class="response-time">${service.responseTimeMs}ms</span>
                </div>
            </div>
        `;
    }).join('');
}

// Render API info
function renderApiInfo(info) {
    const container = document.getElementById('api-info');
    container.innerHTML = `
        <pre>${JSON.stringify(info, null, 2)}</pre>
    `;
}

// Error handler
function showError(elementId, message) {
    const container = document.getElementById(elementId);
    container.innerHTML = `<p style="color: var(--danger-color);">❌ ${message}</p>`;
    container.className = 'status-card';
}

// Format uptime
function formatUptime(seconds) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    if (hours > 0) {
        return `${hours}h ${minutes}m`;
    } else if (minutes > 0) {
        return `${minutes}m ${secs}s`;
    } else {
        return `${secs}s`;
    }
}
