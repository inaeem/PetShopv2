// Minimal vanilla-JS client for the Pet Shop API + a local message board.
const API = ""; // same origin
let token = sessionStorage.getItem("petshop_token") || null;

const $ = (id) => document.getElementById(id);

function setAuth(username, roles) {
    $("authStatus").textContent = username
        ? `Signed in as ${username} (${roles.join(", ")})`
        : "Not signed in";
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
$("loginForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    try {
        const body = await api("/api/auth/login", {
            method: "POST",
            body: JSON.stringify({ username: $("username").value, password: $("password").value })
        });
        token = body.data.accessToken;
        sessionStorage.setItem("petshop_token", token);
        setAuth(body.data.username, body.data.roles);
    } catch (err) {
        alert(err.message);
    }
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
loadPets();
loadMessages();
