const API_BASE = "http://localhost:5033/api/product";

// ── DOM refs ─────────────────────────────────────────────────
const views = {
    list:   document.getElementById("view-list"),
    detail: document.getElementById("view-detail"),
    form:   document.getElementById("view-form"),
};

const grid        = document.getElementById("product-grid");
const noProducts  = document.getElementById("no-products");
const form        = document.getElementById("product-form");
const formTitle   = document.getElementById("form-title");
const submitBtn   = document.getElementById("submit-btn");
const formBackBtn = document.getElementById("form-back-btn");

let editingId = null;      // null = create, number = update
let returnView = "list";   // where "Back" goes after form

// ══════════════════════════════════════════════════════════════
//  VIEW SWITCHING
// ══════════════════════════════════════════════════════════════
function showView(name) {
    Object.values(views).forEach(v => (v.style.display = "none"));
    views[name].style.display = "block";
    window.scrollTo({ top: 0, behavior: "smooth" });

    if (name === "list") loadProducts();
}

// ══════════════════════════════════════════════════════════════
//  VIEW 1 — CARD LIST
// ══════════════════════════════════════════════════════════════
async function loadProducts() {
    try {
        const res = await fetch(`${API_BASE}/getall`);
        const products = await res.json();
        renderCards(products);
    } catch (err) {
        console.error("Failed to load products:", err);
    }
}

function renderCards(products) {
    grid.innerHTML = "";
    if (products.length === 0) {
        noProducts.style.display = "block";
        return;
    }
    noProducts.style.display = "none";

    products.forEach((p) => {
        const card = document.createElement("div");
        card.className = "product-card";
        card.onclick = () => showDetail(p.id);

        const imgHtml = p.imageUrl
            ? `<img class="card-img" src="${p.imageUrl}" alt="${p.name}"
                    onerror="this.outerHTML='<div class=\\'card-img-placeholder\\'>${p.name.charAt(0)}</div>'">`
            : `<div class="card-img-placeholder">${p.name.charAt(0)}</div>`;

        card.innerHTML = `
            ${imgHtml}
            <div class="card-body">
                <span class="badge">${p.category}</span>
                <h3>${p.name}</h3>
                <p class="card-price">$${parseFloat(p.price).toFixed(2)}</p>
            </div>
        `;
        grid.appendChild(card);
    });
}

// ══════════════════════════════════════════════════════════════
//  VIEW 2 — PRODUCT DETAIL
// ══════════════════════════════════════════════════════════════
async function showDetail(id) {
    try {
        const res = await fetch(`${API_BASE}/${id}`);
        const p   = await res.json();

        const detailImg = document.getElementById("detail-img");
        if (p.imageUrl) {
            detailImg.src = p.imageUrl;
            detailImg.style.display = "block";
        } else {
            detailImg.src = "";
            detailImg.style.display = "block"; // gradient background shows via CSS
        }

        document.getElementById("detail-category").textContent    = p.category;
        document.getElementById("detail-name").textContent         = p.name;
        document.getElementById("detail-price").textContent        = `$${parseFloat(p.price).toFixed(2)}`;
        document.getElementById("detail-description").textContent  = p.description || "No description.";

        // Wire edit / delete buttons
        document.getElementById("detail-edit-btn").onclick   = () => openEditForm(p.id);
        document.getElementById("detail-delete-btn").onclick = () => deleteProduct(p.id);

        showView("detail");
    } catch (err) {
        console.error("Failed to load product:", err);
    }
}

// ══════════════════════════════════════════════════════════════
//  VIEW 3 — CREATE / EDIT
// ══════════════════════════════════════════════════════════════
function openCreateForm() {
    editingId = null;
    form.reset();
    formTitle.textContent = "Add New Product";
    submitBtn.textContent = "Add Product";
    returnView = "list";
    formBackBtn.onclick = () => showView("list");
    showView("form");
}

async function openEditForm(id) {
    try {
        const res = await fetch(`${API_BASE}/${id}`);
        const p   = await res.json();

        editingId = p.id;
        document.getElementById("product-name").value        = p.name;
        document.getElementById("product-price").value       = p.price;
        document.getElementById("product-description").value = p.description || "";
        document.getElementById("product-category").value    = p.category;
        document.getElementById("product-image").value       = p.imageUrl || "";

        formTitle.textContent = "Edit Product";
        submitBtn.textContent = "Update Product";
        returnView = "detail";
        formBackBtn.onclick = () => showDetail(id);
        showView("form");
    } catch (err) {
        console.error("Failed to load product for editing:", err);
    }
}

form.addEventListener("submit", async (e) => {
    e.preventDefault();

    const product = {
        name:        document.getElementById("product-name").value.trim(),
        price:       parseFloat(document.getElementById("product-price").value),
        description: document.getElementById("product-description").value.trim(),
        category:    document.getElementById("product-category").value.trim(),
        imageUrl:    document.getElementById("product-image").value.trim(),
    };

    try {
        if (editingId) {
            await fetch(`${API_BASE}/${editingId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(product),
            });
            showDetail(editingId);
        } else {
            await fetch(API_BASE, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(product),
            });
            showView("list");
        }
    } catch (err) {
        console.error("Save failed:", err);
    }
});

// ══════════════════════════════════════════════════════════════
//  DELETE
// ══════════════════════════════════════════════════════════════
async function deleteProduct(id) {
    if (!confirm("Are you sure you want to delete this product?")) return;
    try {
        await fetch(`${API_BASE}/${id}`, { method: "DELETE" });
        showView("list");
    } catch (err) {
        console.error("Delete failed:", err);
    }
}

// ── Init ─────────────────────────────────────────────────────
loadProducts();
