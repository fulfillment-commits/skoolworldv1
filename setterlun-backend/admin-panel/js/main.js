// main.js - Complete Updated Version
let currentPage = 'dashboard';

async function loadComponent(id, file) {
  try {
    const res = await fetch(file);
    const html = await res.text();
    document.getElementById(id).innerHTML = html;
  } catch (e) {
    console.error(`Failed to load component ${file}`, e);
  }
}

async function fetchData(url) {
  try {
    console.log(`[FETCH START] ${url}`);
    const res = await fetch(url, {
      method: 'GET',
      headers: { 'Accept': 'application/json' }
    });
    console.log(`[FETCH STATUS] ${res.status} ${res.statusText} for ${url}`);
    if (!res.ok) {
      throw new Error(`HTTP Error ${res.status}: ${res.statusText}`);
    }
    const data = await res.json();
    console.log(`[FETCH SUCCESS] ${url} → ${Array.isArray(data) ? data.length + ' items' : 'data received'}`);
    return data;
  } catch (err) {
    console.error(`[FETCH FAILED] ${url}:`, err.message);
    throw err;
  }
}

// ==================== DASHBOARD ====================
async function loadDashboardData() {
  try {
    const users = await fetchData(API.users());
    document.getElementById("totalUsers").textContent = users.length || 0;

    const bricks = await fetchData(API.bricks());
    document.getElementById("totalBricks").textContent = bricks.length || 0;

    const companies = await fetchData(API.companies());
    document.getElementById("totalCompanies").textContent = companies.length || 0;

    populateRecentUsers(users.slice(0, 5));
  } catch (e) {
    console.error("Dashboard data load failed", e);
  }
}

function populateRecentUsers(recentUsers) {
  const table = document.getElementById("recentUsersTable");
  if (!table) return;

  let html = `
    <thead>
      <tr class="bg-gray-50 border-b">
        <th class="px-6 py-4 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
        <th class="px-6 py-4 text-left text-xs font-medium text-gray-500 uppercase">Username</th>
        <th class="px-6 py-4 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
        <th class="px-6 py-4 text-left text-xs font-medium text-gray-500 uppercase">Joined</th>
      </tr>
    </thead>
    <tbody class="divide-y divide-gray-200">
  `;

  if (recentUsers.length === 0) {
    html += `<tr><td colspan="4" class="px-6 py-12 text-center text-gray-500">No users yet</td></tr>`;
  } else {
    recentUsers.forEach(user => {
      const joined = user.created_at ? new Date(user.created_at).toLocaleDateString('en-US', {year:'numeric', month:'short', day:'numeric'}) : 'N/A';
      html += `
        <tr class="hover:bg-gray-50">
          <td class="px-6 py-4">${user.full_name || '—'}</td>
          <td class="px-6 py-4 font-medium">${user.username || '—'}</td>
          <td class="px-6 py-4 text-gray-600">${user.email || '—'}</td>
          <td class="px-6 py-4 text-sm text-gray-500">${joined}</td>
        </tr>`;
    });
  }
  html += `</tbody>`;
  table.innerHTML = html;
}

// ==================== USERS PAGE ====================
async function loadUsers() {
  try {
    console.log("[USERS PAGE] Loading users...");
    const users = await fetchData(API.users());

    const tbody = document.getElementById("usersTableBody");
    const countEl = document.getElementById("userCount");

    if (countEl) countEl.textContent = `${users.length} users`;

    let html = '';
    if (users.length === 0) {
      html = `<tr><td colspan="7" class="px-8 py-12 text-center text-gray-500">No users found</td></tr>`;
    } else {
      users.forEach(user => {
        const joined = user.created_at ? new Date(user.created_at).toLocaleDateString('en-US', {year:'numeric', month:'short', day:'numeric'}) : 'N/A';
        
        html += `
          <tr class="hover:bg-gray-50">
            <td class="px-8 py-4">${user.id}</td>
            <td class="px-8 py-4">${user.full_name || '—'}</td>
            <td class="px-8 py-4 font-medium">${user.username || '—'}</td>
            <td class="px-8 py-4">${user.email || '—'}</td>
            <td class="px-8 py-4">${user.phone || '—'}</td>
            <td class="px-8 py-4 text-sm text-gray-500">${joined}</td>
            <td class="px-8 py-4">
              <button onclick="deleteUser(${user.id})" 
                      class="text-red-600 hover:text-red-700 px-3 py-1 rounded hover:bg-red-50 transition">
                <i class="fas fa-trash"></i>
              </button>
            </td>
          </tr>`;
      });
    }

    if (tbody) tbody.innerHTML = html;
    console.log(`[USERS PAGE] Successfully displayed ${users.length} users`);
  } catch (e) {
    console.error("[USERS PAGE] Load failed:", e);
    const tbody = document.getElementById("usersTableBody");
    if (tbody) tbody.innerHTML = `<tr><td colspan="7" class="px-8 py-12 text-center text-red-600">Failed to load users.<br>Check console for details.</td></tr>`;
  }
}

// ==================== USER MANAGEMENT FUNCTIONS ====================

// Delete User
async function deleteUser(userId) {
  if (!confirm("Are you sure you want to delete this user? This action cannot be undone.")) {
    return;
  }

  try {
    const res = await fetch(`${API.users()}/${userId}`, {
      method: 'DELETE'
    });

    if (res.ok) {
      alert("User deleted successfully!");
      loadUsers(); // Refresh table
    } else {
      const errorData = await res.json().catch(() => ({}));
      alert(errorData.error || "Failed to delete user");
    }
  } catch (err) {
    console.error("Delete user error:", err);
    alert("Error deleting user. Please check your connection.");
  }
}

// Show Add User Modal
function showAddUserModal() {
  document.getElementById('addUserModal').classList.remove('hidden');
  document.getElementById('newFullName').focus();
}

function hideAddUserModal() {
  document.getElementById('addUserModal').classList.add('hidden');
  // Clear form
  document.getElementById('newFullName').value = '';
  document.getElementById('newUsername').value = '';
  document.getElementById('newEmail').value = '';
  document.getElementById('newPassword').value = '';
}

// Add New User
async function addNewUser() {
  const fullName = document.getElementById('newFullName').value.trim();
  const username = document.getElementById('newUsername').value.trim();
  const email = document.getElementById('newEmail').value.trim();
  const password = document.getElementById('newPassword').value;

  if (!fullName || !username || !email || !password) {
    alert("Please fill all fields");
    return;
  }

  try {
    const res = await fetch(API.users(), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        full_name: fullName,
        username: username,
        email: email,
        password: password
      })
    });

    if (res.ok) {
      alert("User created successfully!");
      hideAddUserModal();
      loadUsers(); // Refresh the table
    } else {
      const errorData = await res.json().catch(() => ({}));
      alert(errorData.error || "Failed to create user");
    }
  } catch (err) {
    console.error(err);
    alert("Connection error while creating user");
  }
}

// ==================== ADMIN USER MENU ====================

// Toggle User Dropdown
function toggleUserMenu() {
  const dropdown = document.getElementById('userDropdown');
  if (dropdown) dropdown.classList.toggle('hidden');
}

// Close dropdown when clicking outside
document.addEventListener('click', function(event) {
  const userMenu = document.getElementById('userMenu');
  if (userMenu && !userMenu.contains(event.target)) {
    const dropdown = document.getElementById('userDropdown');
    if (dropdown) dropdown.classList.add('hidden');
  }
});

// Set Admin Information Dynamically
function setAdminInfo() {
  const savedInfo = localStorage.getItem('adminInfo');
  
  let name = "Admin";
  let email = "admin@setterlun.com";
  let initial = "A";

  if (savedInfo) {
    try {
      const admin = JSON.parse(savedInfo);
      name = admin.full_name || admin.username || "Admin";
      email = admin.email || "admin@setterlun.com";
      initial = name.charAt(0).toUpperCase();
    } catch (e) {
      console.error("Failed to parse admin info");
    }
  }

  // Update header
  const adminNameEl = document.getElementById('adminName');
  const adminInitialEl = document.getElementById('adminInitial');
  if (adminNameEl) adminNameEl.textContent = name;
  if (adminInitialEl) adminInitialEl.textContent = initial;

  // Update dropdown
  const dropdownNameEl = document.getElementById('dropdownName');
  const dropdownEmailEl = document.getElementById('dropdownEmail');
  if (dropdownNameEl) dropdownNameEl.textContent = name;
  if (dropdownEmailEl) dropdownEmailEl.textContent = email;
}

// Logout Function
function logoutAdmin() {
  if (!confirm("Are you sure you want to logout?")) return;

  localStorage.removeItem('adminToken');
  localStorage.removeItem('adminInfo');
  document.cookie = "adminToken=; path=/; expires=Thu, 01 Jan 1970 00:00:00 UTC";

  window.location.href = '/admin-login.html';
}

// ==================== PAGE NAVIGATION ====================
async function loadPage(page) {
  currentPage = page;
  document.getElementById("pageTitle").textContent = 
    page === 'dashboard' ? 'Dashboard' : 
    page === 'users' ? 'All Users' : 
    page.charAt(0).toUpperCase() + page.slice(1);

  document.querySelectorAll('.nav-link').forEach(link => {
    link.classList.toggle('active', link.textContent.toLowerCase().includes(page));
  });

  if (page === 'dashboard') {
    document.getElementById("content").innerHTML = await (await fetch('dashboard.html')).text();
    loadDashboardData();
  } else if (page === 'users') {
    document.getElementById("content").innerHTML = await (await fetch('users.html')).text();
    loadUsers();
  }
}

async function initAdmin() {
  await loadComponent('sidebar', 'sidebar.html');
  await loadComponent('header', 'header.html');
  await loadComponent('footer', 'footer.html');

  setAdminInfo();        // Set dynamic admin name & email
  loadPage('dashboard');
}

window.onload = initAdmin;