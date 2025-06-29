/* global $, fetch */
let currentPage = 1;
let currentSearch = "";
const pageSize = 20;

/* ----------  load first page on DOM ready  ---------- */
document.addEventListener("DOMContentLoaded", () => {
  loadRecords();
  document
    .getElementById("searchInput")
    .addEventListener("keypress", (e) => e.key === "Enter" && searchRecords());
});

/* ----------  fetch + render records  ---------- */
async function loadRecords(page = 1, search = "") {
  try {
    showLoading(true);
    const url = `/Collection/GetCollectionRecords?page=${page}` +
      `&pageSize=${pageSize}&search=${encodeURIComponent(search)}`;

    const res = await fetch(url);
    const json = await res.json();
    if (!json.success) throw new Error(json.message);

    displayRecords(json.records);
    displayPagination(json.pagination);

    currentPage = page;
    currentSearch = search;
  } catch (err) {
    showError(err.message);
  } finally {
    showLoading(false);
  }
}

/* ----------  render helpers  ---------- */
function displayRecords(records) {
  const tbody = document.getElementById("recordsTableBody");

  if (!records.length) {
    tbody.innerHTML = `
      <tr>
        <td colspan="9" class="text-center py-4 text-muted">
          <i class="fas fa-inbox fa-3x mb-2"></i><br>No collection records found
        </td>
      </tr>`;
    return;
  }

  tbody.innerHTML = records
    .map((r) => `
      <tr>
        <td><strong class="text-primary">${r.binPlateId}</strong><br>
            <small class="text-muted">Zone: ${r.binZone}</small></td>

        <td>${r.binLocation}<br>
            <small class="text-muted">Fill: ${r.fillLevel}%</small></td>

        <td><span class="badge badge-info">${r.clientName}</span></td>
        <td><i class="fas fa-user text-muted"></i> ${r.collectorName}</td>
        <td><i class="fas fa-truck text-muted"></i> ${r.truckLicensePlate}</td>

        <td><strong>${r.pickupDate}</strong><br>
            <small class="text-muted">${r.pickupTime}</small></td>

        <td>${r.gpsLatitude
        ? `<a href="https://maps.google.com/?q=${r.gpsLatitude},${r.gpsLongitude}" target="_blank">
                 <i class="fas fa-map-marker-alt"></i> View
               </a>`
        : "<span class='text-muted'>No GPS</span>"
      }</td>

        <td>${r.hasImage
        ? '<span class="badge badge-info"><i class="fas fa-camera"></i> Available</span>'
        : '<span class="badge badge-secondary"><i class="fas fa-times"></i> No Image</span>'
      }</td>


        <td>${r.hasImage
        ? `<button class="btn btn-sm btn-primary"
                       onclick="viewImage('${r.imageId}','${r.binPlateId}','${r.pickupTimestamp}',
                                          '${r.binLocation}','${r.clientName}','${r.collectorName}')">
                   <i class="fas fa-eye"></i> View
               </button>`
        : `<button class="btn btn-sm btn-secondary" disabled>
                   <i class="fas fa-ban"></i> No Image
               </button>`
      }</td>
      </tr>`)
    .join("");
}

function displayPagination(p) {
  const el = document.getElementById("pagination");
  if (p.totalPages <= 1) return (el.innerHTML = "");

  let html = "";
  const pageBtn = (n, label = n) =>
    `<li class="page-item ${n === p.currentPage ? "active" : ""}">
       <a class="page-link" href="#" onclick="loadRecords(${n}, '${currentSearch}')">${label}</a>
     </li>`;

  if (p.hasPreviousPage) html += pageBtn(p.currentPage - 1, '<i class="fas fa-chevron-left"></i>');

  const start = Math.max(1, p.currentPage - 2);
  const end = Math.min(p.totalPages, p.currentPage + 2);

  if (start > 1) {
    html += pageBtn(1);
    if (start > 2) html += '<li class="page-item disabled"><span class="page-link">…</span></li>';
  }

  for (let i = start; i <= end; i++) html += pageBtn(i);

  if (end < p.totalPages) {
    if (end < p.totalPages - 1) html += '<li class="page-item disabled"><span class="page-link">…</span></li>';
    html += pageBtn(p.totalPages);
  }

  if (p.hasNextPage) html += pageBtn(p.currentPage + 1, '<i class="fas fa-chevron-right"></i>');

  el.innerHTML = html;
}

/* ----------  modal image viewer  ---------- */
function viewImage(id, plate, time, loc, client, collector) {
  const modal = $("#imageModal");
  const imgEl = document.getElementById("collectionImage");
  const detailsEl = document.getElementById("imageDetails");
  const dlBtn = document.getElementById("downloadImageBtn");
  const loading = document.getElementById("imageLoadingIndicator");

  imgEl.style.display = "none";
  detailsEl.style.display = "none";
  dlBtn.style.display = "none";
  loading.style.display = "block";

  document.getElementById("imageModalLabel").textContent = `📸 ${plate}`;
  modal.modal("show");

  const src = `/Collection/GetImage/${id}`;
  imgEl.onload = () => {
    loading.style.display = "none";
    imgEl.style.display = "block";
    detailsEl.style.display = "block";
    dlBtn.style.display = "inline-block";
    dlBtn.href = src;
    dlBtn.download = `collection_${plate}_${time.replace(/[/:]/g, "-")}.jpg`;

    detailsEl.innerHTML = `
      <p><strong>Plate:</strong> ${plate}</p>
      <p><strong>Location:</strong> ${loc}</p>
      <p><strong>Client:</strong> ${client}</p>
      <p><strong>Collector:</strong> ${collector}</p>
      <p><strong>Time:</strong> ${time}</p>
      <p><strong>ID:</strong> ${id}</p>`;
  };
  imgEl.onerror = () => {
    loading.style.display = "none";
    detailsEl.style.display = "block";
    detailsEl.innerHTML = '<div class="alert alert-danger">Failed to load image.</div>';
  };
  imgEl.src = src;
}

/* ----------  misc helpers  ---------- */
function searchRecords() { loadRecords(1, document.getElementById("searchInput").value.trim()); }
function clearSearch() { document.getElementById("searchInput").value = ""; loadRecords(1, ""); }
function showLoading(b) { document.getElementById("loadingIndicator").style.display = b ? "block" : "none"; }
function showError(msg) { console.error(msg); alert(msg); }
