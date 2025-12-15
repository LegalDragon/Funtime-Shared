// State
let authToken = localStorage.getItem('authToken') || null;
let currentUser = null;

// DOM Elements
const responseLog = document.getElementById('responseLog');
const userStatus = document.getElementById('userStatus');
const userInfo = document.getElementById('userInfo');

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    initializeTabs();
    initializeForms();

    if (authToken) {
        getMe();
    }
});

// Tab Navigation
function initializeTabs() {
    const tabBtns = document.querySelectorAll('.tab-btn');
    const tabPanes = document.querySelectorAll('.tab-pane');

    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const tabId = btn.dataset.tab;

            tabBtns.forEach(b => b.classList.remove('active'));
            tabPanes.forEach(p => p.classList.remove('active'));

            btn.classList.add('active');
            document.getElementById(tabId).classList.add('active');
        });
    });
}

// Form Handlers
function initializeForms() {
    // Register
    document.getElementById('registerForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const email = document.getElementById('regEmail').value;
        const password = document.getElementById('regPassword').value;
        await apiCall('POST', '/auth/register', { email, password });
    });

    // Login
    document.getElementById('loginForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const email = document.getElementById('loginEmail').value;
        const password = document.getElementById('loginPassword').value;
        await apiCall('POST', '/auth/login', { email, password });
    });

    // Validate Token
    document.getElementById('validateForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const token = document.getElementById('validateToken').value;
        await apiCall('POST', '/auth/validate', { token }, false);
    });

    // Send OTP
    document.getElementById('sendOtpForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const phoneNumber = document.getElementById('otpPhone').value;
        await apiCall('POST', '/auth/otp/send', { phoneNumber }, false);
    });

    // Verify OTP
    document.getElementById('verifyOtpForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const phoneNumber = document.getElementById('verifyPhone').value;
        const code = document.getElementById('verifyCode').value;
        await apiCall('POST', '/auth/otp/verify', { phoneNumber, code });
    });

    // Link Phone
    document.getElementById('linkPhoneForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const phoneNumber = document.getElementById('linkPhoneNumber').value;
        const code = document.getElementById('linkPhoneCode').value;
        await apiCall('POST', '/auth/link-phone', { phoneNumber, code }, true);
    });

    // Link Email
    document.getElementById('linkEmailForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const email = document.getElementById('linkEmail').value;
        const password = document.getElementById('linkEmailPassword').value;
        await apiCall('POST', '/auth/link-email', { email, password }, true);
    });

    // Link External
    document.getElementById('linkExternalForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const provider = document.getElementById('linkProvider').value;
        const providerUserId = document.getElementById('linkProviderId').value;
        const providerEmail = document.getElementById('linkProviderEmail').value || null;
        const providerDisplayName = document.getElementById('linkProviderName').value || null;
        await apiCall('POST', '/auth/link-external', {
            provider,
            providerUserId,
            providerEmail,
            providerDisplayName
        }, true);
    });

    // Unlink External
    document.getElementById('unlinkExternalForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const provider = document.getElementById('unlinkProvider').value;
        await apiCall('POST', '/auth/unlink-external', { provider }, true);
    });

    // Change Password
    document.getElementById('changePasswordForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const currentPassword = document.getElementById('currentPassword').value;
        const newPassword = document.getElementById('newPassword').value;
        await apiCall('POST', '/auth/change-password', { currentPassword, newPassword }, true);
    });

    // Reset Password
    document.getElementById('resetPasswordForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const phoneNumber = document.getElementById('resetPhone').value;
        const code = document.getElementById('resetCode').value;
        const newPassword = document.getElementById('resetNewPassword').value;
        await apiCall('POST', '/auth/reset-password', { phoneNumber, code, newPassword }, false);
    });
}

// API Call Helper
async function apiCall(method, endpoint, body = null, requireAuth = false) {
    const baseUrl = document.getElementById('apiUrl').value.replace(/\/$/, '');
    const url = baseUrl + endpoint;

    const headers = {
        'Content-Type': 'application/json'
    };

    if (requireAuth && authToken) {
        headers['Authorization'] = `Bearer ${authToken}`;
    }

    const options = {
        method,
        headers
    };

    if (body && method !== 'GET') {
        options.body = JSON.stringify(body);
    }

    try {
        logRequest(method, endpoint, body);

        const response = await fetch(url, options);
        const data = await response.json();

        if (response.ok) {
            logResponse(endpoint, data, 'success');
            handleSuccessResponse(endpoint, data);
        } else {
            logResponse(endpoint, data, 'error');
        }

        return data;
    } catch (error) {
        logResponse(endpoint, { error: error.message }, 'error');
        return null;
    }
}

// Handle successful responses
function handleSuccessResponse(endpoint, data) {
    // Store token if present
    if (data.token) {
        authToken = data.token;
        localStorage.setItem('authToken', authToken);
    }

    // Update user info if present
    if (data.user) {
        currentUser = data.user;
        updateUserStatus();
    }

    // Handle specific endpoints
    if (endpoint === '/auth/me' || endpoint.includes('/auth/register') ||
        endpoint.includes('/auth/login') || endpoint.includes('/auth/otp/verify')) {
        if (data.user || data.id) {
            currentUser = data.user || data;
            updateUserStatus();
        }
    }
}

// Get Me
async function getMe() {
    if (!authToken) {
        logResponse('/auth/me', { error: 'Not logged in' }, 'error');
        return;
    }

    const data = await apiCall('GET', '/auth/me', null, true);
    if (data && !data.success === false) {
        currentUser = data;
        updateUserStatus();
    }
}

// Get External Logins
async function getExternalLogins() {
    if (!authToken) {
        logResponse('/auth/external-logins', { error: 'Not logged in' }, 'error');
        return;
    }

    const data = await apiCall('GET', '/auth/external-logins', null, true);

    const listDiv = document.getElementById('externalLoginsList');
    if (data && Array.isArray(data)) {
        if (data.length === 0) {
            listDiv.innerHTML = '<div class="result-item">No external logins linked</div>';
        } else {
            listDiv.innerHTML = data.map(login => `
                <div class="result-item">
                    <strong>${login.provider}</strong><br>
                    ID: ${login.providerUserId}<br>
                    ${login.providerEmail ? `Email: ${login.providerEmail}<br>` : ''}
                    ${login.providerDisplayName ? `Name: ${login.providerDisplayName}<br>` : ''}
                    Linked: ${new Date(login.createdAt).toLocaleDateString()}
                </div>
            `).join('');
        }
    }
}

// Update User Status Display
function updateUserStatus() {
    if (currentUser) {
        userStatus.classList.remove('hidden');
        userInfo.innerHTML = `
ID: ${currentUser.id}
Email: ${currentUser.email || 'Not set'}
Phone: ${currentUser.phoneNumber || 'Not set'}
Email Verified: ${currentUser.isEmailVerified}
Phone Verified: ${currentUser.isPhoneVerified}
Created: ${new Date(currentUser.createdAt).toLocaleString()}
Last Login: ${currentUser.lastLoginAt ? new Date(currentUser.lastLoginAt).toLocaleString() : 'N/A'}
        `.trim();
    } else {
        userStatus.classList.add('hidden');
    }
}

// Logout
function logout() {
    authToken = null;
    currentUser = null;
    localStorage.removeItem('authToken');
    userStatus.classList.add('hidden');
    logResponse('logout', { message: 'Logged out successfully' }, 'info');
}

// Logging
function logRequest(method, endpoint, body) {
    const entry = document.createElement('div');
    entry.className = 'log-entry info';
    entry.innerHTML = `
        <div class="log-timestamp">${new Date().toLocaleTimeString()}</div>
        <div class="log-endpoint">${method} ${endpoint}</div>
        <div class="log-content">${body ? JSON.stringify(body, null, 2) : 'No body'}</div>
    `;
    responseLog.insertBefore(entry, responseLog.firstChild);
}

function logResponse(endpoint, data, type) {
    const entry = document.createElement('div');
    entry.className = `log-entry ${type}`;
    entry.innerHTML = `
        <div class="log-timestamp">${new Date().toLocaleTimeString()}</div>
        <div class="log-endpoint">Response: ${endpoint}</div>
        <div class="log-content">${JSON.stringify(data, null, 2)}</div>
    `;
    responseLog.insertBefore(entry, responseLog.firstChild);
}

function clearLog() {
    responseLog.innerHTML = '';
}
