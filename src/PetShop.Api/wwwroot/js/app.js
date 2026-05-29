// Minimal vanilla-JS client for the Pet Shop API + a local message board.
const API = ""; // same origin
let token = sessionStorage.getItem("petshop_token") || null;

const $ = (id) => document.getElementById(id);

function setAuth(active) {
    $("authStatus").textContent = active ? "Token set" : "No token";
}

async function api(path, options = {}) {
    const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
    if (token) headers["Authorization"] = `Bearer ${token}`;
    const res = await fetch(API + path, { ...options, headers });
    const body = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(body.message || `Request failed (${res.status})`);
    return body;
}

// ---- Auth ----
// The API no longer issues tokens — the user pastes an externally-obtained JWT.
$("tokenForm").addEventListener("submit", (e) => {
    e.preventDefault();
    token = $("tokenInput").value.trim() || null;
    if (token) sessionStorage.setItem("petshop_token", token);
    else sessionStorage.removeItem("petshop_token");
    setAuth(!!token);
    loadPets();
});

$("clearTokenBtn").addEventListener("click", () => {
    token = null;
    sessionStorage.removeItem("petshop_token");
    $("tokenInput").value = "";
    setAuth(false);
    $("petList").innerHTML = "<li>Provide a token to view pets.</li>";
});

// ---- Pets ----
function renderPets(pets) {
    $("petList").innerHTML = pets.map(p => `
        <li>
            <span>
                <strong>${p.name}</strong>
                <span class="breed">${p.breed ?? p.categoryName ?? ""}</span>
            </span>
            <span class="price">$${Number(p.price).toFixed(2)}</span>
        </li>`).join("") || "<li>No pets found.</li>";
}

async function loadPets() {
    try {
        const body = await api("/api/pets?page=1&pageSize=50");
        renderPets(body.data.items);
    } catch (err) { alert(err.message); }
}

async function searchPets() {
    try {
        const term = encodeURIComponent($("searchTerm").value || "");
        const body = await api(`/api/pets/search?term=${term}`);
        renderPets(body.data);
    } catch (err) { alert(err.message); }
}

$("searchBtn").addEventListener("click", searchPets);
$("refreshBtn").addEventListener("click", loadPets);

// ---- Local message board ----
function loadMessages() {
    const msgs = JSON.parse(localStorage.getItem("petshop_messages") || "[]");
    $("messageList").innerHTML = msgs.map(m => `
        <li><span class="time">${new Date(m.t).toLocaleString()}</span>${m.text}</li>`).join("");
}

$("messageForm").addEventListener("submit", (e) => {
    e.preventDefault();
    const text = $("messageInput").value.trim();
    if (!text) return;
    const msgs = JSON.parse(localStorage.getItem("petshop_messages") || "[]");
    msgs.unshift({ text, t: Date.now() });
    localStorage.setItem("petshop_messages", JSON.stringify(msgs.slice(0, 50)));
    $("messageInput").value = "";
    loadMessages();
});

// ---- Init ----
if (token) $("tokenInput").value = token;
setAuth(!!token);
if (token) loadPets();
else $("petList").innerHTML = "<li>Provide a token to view pets.</li>";
loadMessages();
