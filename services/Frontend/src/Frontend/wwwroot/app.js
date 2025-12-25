(function(){
  function qs(sel, el){ return (el||document).querySelector(sel); }
  function qsa(sel, el){ return Array.from((el||document).querySelectorAll(sel)); }

  function toast(type, title, message){
    const wrap = qs("#toastWrap") || (() => {
      const d = document.createElement("div");
      d.id = "toastWrap";
      d.className = "toast-wrap";
      document.body.appendChild(d);
      return d;
    })();

    const t = document.createElement("div");
    t.className = "toast " + (type || "");
    t.innerHTML = `
      <div class="icon"></div>
      <div>
        <div class="title">${escapeHtml(title || "Info")}</div>
        <div class="msg">${escapeHtml(message || "")}</div>
      </div>
    `;
    wrap.appendChild(t);

    setTimeout(() => { t.style.opacity = "0"; t.style.transform = "translateY(-6px)"; }, 3800);
    setTimeout(() => { t.remove(); }, 4200);
  }

  function escapeHtml(s){
    return (s ?? "").toString()
      .replaceAll("&","&amp;")
      .replaceAll("<","&lt;")
      .replaceAll(">","&gt;")
      .replaceAll('"',"&quot;")
      .replaceAll("'","&#039;");
  }

  function initUserIdHistory(){
    const input = qs("#userIdInput");
    if (!input) return;

    const key = "gozon.userIds";
    let list = [];
    try { list = JSON.parse(localStorage.getItem(key) || "[]"); } catch { list = []; }

    const datalist = qs("#userIdHistory");
    if (datalist){
      datalist.innerHTML = "";
      list.slice(0, 8).forEach(x => {
        const o = document.createElement("option");
        o.value = x;
        datalist.appendChild(o);
      });
    }

    const form = qs("#userIdForm");
    if (form){
      form.addEventListener("submit", () => {
        const value = (input.value || "").trim();
        if (!value) return;
        const next = [value, ...list.filter(x => x !== value)].slice(0, 12);
        localStorage.setItem(key, JSON.stringify(next));
      });
    }
  }

  function initClientValidations(){
    qsa("[data-validate=amount]").forEach(inp => {
      inp.addEventListener("input", () => {
        const v = parseFloat(inp.value || "0");
        const ok = Number.isFinite(v) && v > 0;
        inp.style.borderColor = ok ? "" : "rgba(239,68,68,0.7)";
      });
    });

    qsa("form[data-validate-form=true]").forEach(form => {
      form.addEventListener("submit", (e) => {
        const amount = qs("[data-validate=amount]", form);
        if (amount){
          const v = parseFloat(amount.value || "0");
          if (!Number.isFinite(v) || v <= 0){
            e.preventDefault();
            toast("err", "Validation", "Amount must be > 0");
            amount.focus();
          }
        }
      });
    });
  }

  async function copyText(text){
    try{
      await navigator.clipboard.writeText(text);
      toast("ok", "Copied", text);
    }catch{
      toast("warn", "Copy failed", "Clipboard API is not available.");
    }
  }

  function wireCopyButtons(){
    qsa("[data-copy]").forEach(btn => {
      btn.addEventListener("click", () => {
        copyText(btn.getAttribute("data-copy") || "");
      });
    });
  }

  function initOrdersAutoRefresh(){
    const table = qs("#ordersTable");
    const toggle = qs("#autoRefreshToggle");
    const status = qs("#autoRefreshStatus");
    const endpoint = (qs("#ordersJsonEndpoint") || {}).value;

    if (!table || !toggle || !endpoint) return;

    let timer = null;
    let enabled = (toggle.getAttribute("data-enabled") || "true") === "true";

    function setEnabled(v){
      enabled = v;
      toggle.textContent = enabled ? "Pause auto-refresh" : "Resume auto-refresh";
      if (status) status.textContent = enabled ? "Auto-refresh: ON" : "Auto-refresh: OFF";
      toggle.setAttribute("data-enabled", enabled ? "true" : "false");
      if (timer) clearInterval(timer);
      if (enabled){
        timer = setInterval(refresh, 2500);
      }
    }

    toggle.addEventListener("click", () => setEnabled(!enabled));

    async function refresh(){
      try{
        const resp = await fetch(endpoint, { headers: { "Accept": "application/json" } });
        if (!resp.ok) return;
        const data = await resp.json();
        renderOrders(data || []);
      }catch{}
    }

    function badgeClass(status){
      const s = (status || "").toLowerCase();
      if (s === "finished") return "ok";
      if (s === "cancelled") return "err";
      return "warn";
    }

    function renderOrders(items){
      const body = qs("tbody", table);
      if (!body) return;

      const filterText = (qs("#ordersSearch") || {}).value || "";
      const statusFilter = (qs("#ordersStatusFilter") || {}).value || "all";

      let filtered = items.slice();
      if (filterText.trim()){
        const t = filterText.trim().toLowerCase();
        filtered = filtered.filter(x => (x.orderId || "").toLowerCase().includes(t));
      }
      if (statusFilter !== "all"){
        filtered = filtered.filter(x => (x.status || "").toLowerCase() === statusFilter);
      }

      const sort = (qs("#ordersSort") || {}).value || "created_desc";
      filtered.sort((a,b) => {
        const da = new Date(a.createdAtUtc);
        const db = new Date(b.createdAtUtc);
        if (sort === "created_asc") return da - db;
        return db - da;
      });

      body.innerHTML = "";
      for (const o of filtered){
        const tr = document.createElement("tr");
        const id = o.orderId;
        tr.innerHTML = `
          <td class="mono">
            <a href="/Order/${id}">${id}</a>
            <button class="btn small ghost" style="margin-left:8px" data-copy="${id}">Copy</button>
          </td>
          <td>${o.amount}</td>
          <td><span class="badge dot ${badgeClass(o.status)}">${o.status}</span></td>
          <td class="small-muted">${new Date(o.createdAtUtc).toLocaleString()}</td>
        `;
        body.appendChild(tr);
      }

      wireCopyButtons();
    }
    
    ["#ordersSearch", "#ordersStatusFilter", "#ordersSort"].forEach(sel => {
      const el = qs(sel);
      if (el) el.addEventListener("input", refresh);
      if (el) el.addEventListener("change", refresh);
    });

    refresh();
    setEnabled(enabled);
  }

  function initOrderDetailsAutoRefresh(){
    const endpoint = (qs("#orderJsonEndpoint") || {}).value;
    const statusEl = qs("#orderStatusValue");
    const updatedEl = qs("#orderUpdatedValue");
    const refreshBtn = qs("#orderRefreshBtn");
    const badgeWrap = qs("#orderStatusBadgeWrap");

    if (!endpoint || !statusEl) return;

    let timer = null;

    async function refresh(){
      try{
        const resp = await fetch(endpoint, { headers: { "Accept": "application/json" } });
        if (!resp.ok) return;
        const o = await resp.json();
        if (!o) return;

        statusEl.textContent = o.status || "";
        if (updatedEl) updatedEl.textContent = new Date(o.updatedAtUtc).toLocaleString();

        if (badgeWrap){
          badgeWrap.innerHTML = renderBadge(o.status || "");
        }

        const s = (o.status || "").toLowerCase();
        const isPending = s === "new";
        if (!isPending && timer){
          clearInterval(timer);
          timer = null;
          toast("ok", "Order updated", "Final status: " + (o.status || ""));
        }
      }catch{}
    }

    function renderBadge(status){
      const s = (status || "").toLowerCase();
      let cls = "warn";
      if (s === "finished") cls = "ok";
      if (s === "cancelled") cls = "err";
      return `<span class="badge dot ${cls}">${escapeHtml(status)}</span>`;
    }

    if (refreshBtn) refreshBtn.addEventListener("click", (e) => { e.preventDefault(); refresh(); });

    refresh();

    const initStatus = (statusEl.textContent || "").toLowerCase();
    if (initStatus === "new"){
      timer = setInterval(refresh, 2500);
    }
  }
  window.gozon = { toast };

  document.addEventListener("DOMContentLoaded", () => {
    initUserIdHistory();
    initClientValidations();
    wireCopyButtons();
    initOrdersAutoRefresh();
    initOrderDetailsAutoRefresh();
    const serverToast = qs("#serverToast");
    if (serverToast){
      const type = serverToast.getAttribute("data-type") || "";
      const title = serverToast.getAttribute("data-title") || "Info";
      const msg = serverToast.getAttribute("data-message") || "";
      if (msg) toast(type, title, msg);
    }
  });
})();
