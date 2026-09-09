// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/* ==========================================================================
   Wishlist Sidebar
   ========================================================================== */
(function () {
  "use strict";

  const toggleBtn = document.getElementById("wishlist-toggle-btn");
  const sidebar = document.getElementById("wishlist-sidebar");
  const backdrop = document.getElementById("wishlist-backdrop");
  const closeBtn = document.getElementById("wishlist-close-btn");
  const contentEl = document.getElementById("wishlist-content");
  const badge = document.getElementById("wishlist-badge");

  // Only wire up if the heart button exists (signed-in non-admin users)
  if (!toggleBtn || !sidebar) return;

  // ── Helpers ──────────────────────────────────────────────────────────────

  function formatCurrency(amount) {
    return "Rs. " + Number(amount).toLocaleString("en-NP");
  }

  function formatDate(dateStr) {
    const d = new Date(dateStr);
    const now = new Date();
    const diffMs = d - now;
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 0) return "Ended";
    if (diffDays === 0) return "Ends today";
    if (diffDays === 1) return "Ends tomorrow";
    if (diffDays < 7) return `Ends in ${diffDays} days`;
    return (
      "Ends " +
      d.toLocaleDateString("en-NP", {
        day: "numeric",
        month: "short",
        year: "numeric",
      })
    );
  }

  function updateBadge(count) {
    if (!badge) return;
    if (count > 0) {
      badge.textContent = count > 99 ? "99+" : count;
      badge.style.display = "block";
      toggleBtn.classList.add("has-items");
    } else {
      badge.style.display = "none";
      toggleBtn.classList.remove("has-items");
    }
  }

  function getAntiForgeryToken() {
    const el = document.querySelector(
      'input[name="__RequestVerificationToken"]',
    );
    return el ? el.value : "";
  }

  // ── Render Items ─────────────────────────────────────────────────────────

  function renderItems(items) {
    if (!items || items.length === 0) {
      contentEl.innerHTML = `
                <div class="wishlist-empty">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
                         fill="none" stroke="#ccc" stroke-width="1.5"
                         stroke-linecap="round" stroke-linejoin="round" width="56" height="56">
                        <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>
                    </svg>
                    <p><strong>Your wishlist is empty</strong>Browse auctions and save items you love!</p>
                </div>`;
      updateBadge(0);
      return;
    }

    // Footer CTA
    const footer = document.createElement("div");
    footer.className = "wishlist-sidebar-footer";
    footer.innerHTML = `<a href="/AuctionCatalog">Browse All Auctions</a>`;

    const fragment = document.createDocumentFragment();

    items.forEach(function (item, idx) {
      const card = document.createElement("div");
      card.className = "wishlist-item-card";
      card.style.animationDelay = idx * 50 + "ms";
      card.dataset.auctionItemId = item.auctionItemId;

      const thumbHtml = item.thumbnailUrl
        ? `<img src="${escapeHtml(item.thumbnailUrl)}" alt="${escapeHtml(item.title)}" class="wishlist-item-thumb" loading="lazy" decoding="async">`
        : `<div class="wishlist-item-thumb-placeholder">
                       <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none"
                            stroke="currentColor" stroke-width="1.5" width="28" height="28">
                           <rect x="3" y="3" width="18" height="18" rx="3" ry="3"/>
                           <circle cx="8.5" cy="8.5" r="1.5"/>
                           <polyline points="21 15 16 10 5 21"/>
                       </svg>
                   </div>`;

      card.innerHTML = `
                ${thumbHtml}
                <a href="/AuctionCatalog/Details/${item.auctionItemId}" class="wishlist-item-body text-decoration-none">
                    <div class="wishlist-item-title">${escapeHtml(item.title)}</div>
                    <div class="wishlist-item-price">${formatCurrency(item.reservePrice)}</div>
                    <div class="wishlist-item-meta">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none"
                             stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                             width="11" height="11">
                            <circle cx="12" cy="12" r="10"/>
                            <polyline points="12 6 12 12 16 14"/>
                        </svg>
                        ${formatDate(item.auctionEndDate)}
                    </div>
                </a>
                <button class="wishlist-item-remove"
                        aria-label="Remove from wishlist"
                        title="Remove"
                        data-auction-item-id="${item.auctionItemId}">
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"
                         width="12" height="12">
                        <line x1="18" y1="6" x2="6" y2="18"/>
                        <line x1="6" y1="6" x2="18" y2="18"/>
                    </svg>
                </button>`;

      fragment.appendChild(card);
    });

    contentEl.innerHTML = "";
    contentEl.appendChild(fragment);

    // Append footer inside sidebar (after content)
    const existingFooter = sidebar.querySelector(".wishlist-sidebar-footer");
    if (existingFooter) existingFooter.remove();
    sidebar.appendChild(footer);

    updateBadge(items.length);

    // Wire up remove buttons
    contentEl.querySelectorAll(".wishlist-item-remove").forEach(function (btn) {
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();
        const id = parseInt(btn.dataset.auctionItemId);
        removeItem(id, btn.closest(".wishlist-item-card"));
      });
    });
  }

  // ── Fetch Wishlist ────────────────────────────────────────────────────────

  function loadWishlist() {
    contentEl.innerHTML = `
            <div class="wishlist-loading">
                <div class="wishlist-spinner"></div>
                <p>Loading your wishlist\u2026</p>
            </div>`;

    // Remove any previous footer
    const existingFooter = sidebar.querySelector(".wishlist-sidebar-footer");
    if (existingFooter) existingFooter.remove();

    fetch("/SavedListing/GetWishlistPartial", {
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" },
    })
      .then(function (res) {
        if (!res.ok) throw new Error("Failed to load wishlist");
        return res.json();
      })
      .then(function (data) {
        renderItems(data);
      })
      .catch(function () {
        contentEl.innerHTML = `
                <div class="wishlist-empty">
                    <p><strong>Could not load wishlist</strong>Please try again.</p>
                </div>`;
      });
  }

  // ── Remove Item ───────────────────────────────────────────────────────────

  function removeItem(auctionItemId, cardEl) {
    const token = getAntiForgeryToken();

    // Optimistic UI: fade the card out
    if (cardEl) {
      cardEl.style.transition = "opacity 0.25s ease, transform 0.25s ease";
      cardEl.style.opacity = "0";
      cardEl.style.transform = "translateX(20px)";
      setTimeout(function () {
        cardEl.remove();
      }, 260);
    }

    fetch("/SavedListing/Toggle", {
      method: "POST",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        RequestVerificationToken: token,
      },
      body: "auctionItemId=" + encodeURIComponent(auctionItemId),
    })
      .then(function (res) {
        return res.json();
      })
      .then(function () {
        // Recount visible cards
        const remaining = contentEl.querySelectorAll(
          ".wishlist-item-card",
        ).length;
        updateBadge(remaining);
        if (remaining === 0) {
          setTimeout(function () {
            renderItems([]);
          }, 280);
        }
      })
      .catch(function () {
        // On error, reload to be safe
        loadWishlist();
      });
  }

  // ── Load Badge Count on Page Load ─────────────────────────────────────────

  fetch("/SavedListing/GetWishlistCount", {
    credentials: "same-origin",
    headers: { "X-Requested-With": "XMLHttpRequest" },
  })
    .then(function (res) {
      return res.json();
    })
    .then(function (data) {
      updateBadge(data.count);
    })
    .catch(function () {
      /* silently ignore */
    });

  // ── Open / Close ──────────────────────────────────────────────────────────

  function openSidebar() {
    sidebar.classList.add("open");
    backdrop.classList.add("active");
    toggleBtn.classList.add("is-open");
    document.body.style.overflow = "hidden"; // prevent body scroll
    loadWishlist();
  }

  function closeSidebar() {
    sidebar.classList.remove("open");
    backdrop.classList.remove("active");
    toggleBtn.classList.remove("is-open");
    document.body.style.overflow = "";
  }

  toggleBtn.addEventListener("click", function () {
    if (sidebar.classList.contains("open")) {
      closeSidebar();
    } else {
      openSidebar();
    }
  });

  closeBtn.addEventListener("click", closeSidebar);
  backdrop.addEventListener("click", closeSidebar);

  // Close on Escape key
  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && sidebar.classList.contains("open")) {
      closeSidebar();
    }
  });

  // ── Utility ───────────────────────────────────────────────────────────────

  function escapeHtml(str) {
    const div = document.createElement("div");
    div.appendChild(document.createTextNode(str || ""));
    return div.innerHTML;
  }
})();
