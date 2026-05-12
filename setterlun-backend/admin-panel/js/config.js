const CONFIG = {
  API_BASE_URL: ''   // Relative URLs (best when served from same server)
};

const API = {
  users: () => `${CONFIG.API_BASE_URL}/users`,
  companies: () => `${CONFIG.API_BASE_URL}/companies`,
  bricks: () => `${CONFIG.API_BASE_URL}/bricks`
};

window.CONFIG = CONFIG;
window.API = API;