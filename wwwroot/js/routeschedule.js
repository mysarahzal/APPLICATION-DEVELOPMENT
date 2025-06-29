document.addEventListener("DOMContentLoaded", () => {
  let map = null
  let routingControl = null
  let markers = []
  const L = window.L // Declare the L variable

  const scheduleSelect = document.getElementById("scheduleSelect")
  const scheduleHeader = document.getElementById("scheduleHeader")
  const mapSection = document.getElementById("mapSection")
  const checkpointsList = document.getElementById("checkpointsList")

  // Handle schedule selection
  scheduleSelect.addEventListener("change", function () {
    const scheduleId = this.value

    if (scheduleId) {
      const selectedOption = this.options[this.selectedIndex]
      const truckName = selectedOption.dataset.truck
      const routeName = selectedOption.dataset.route
      const pointCount = selectedOption.dataset.count

      // Update header
      document.getElementById("routeName").textContent = routeName
      document.getElementById("truckName").textContent = truckName
      document.getElementById("checkpointBadge").textContent = `${pointCount} Checkpoints`

      // Show sections
      scheduleHeader.style.display = "block"
      mapSection.style.display = "block"

      // Initialize map if needed
      if (!map) {
        initializeMap()
      }

      // Load collection points
      loadCollectionPoints(scheduleId)
    } else {
      // Hide sections
      scheduleHeader.style.display = "none"
      mapSection.style.display = "none"
    }
  })

  function initializeMap() {
    const mapContainer = document.querySelector("#scheduleMap")
    if (!mapContainer) return

    map = L.map("scheduleMap").setView([1.52, 103.85], 13)

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: "&copy; OpenStreetMap contributors",
    }).addTo(map)
  }

  function loadCollectionPoints(scheduleId) {
    checkpointsList.innerHTML = '<p class="text-muted">Loading checkpoints...</p>'

    fetch(`/RouteSchedule/GetCollectionPoints?scheduleId=${scheduleId}`)
      .then((response) => {
        if (!response.ok) {
          throw new Error("Network response was not ok")
        }
        return response.json()
      })
      .then((data) => {
        displayCollectionPoints(data.collectionPoints)
      })
      .catch((error) => {
        console.error("Error loading collection points:", error)
        checkpointsList.innerHTML = '<p class="text-danger">Error loading checkpoints. Please try again.</p>'
      })
  }

  function displayCollectionPoints(collectionPoints) {
    // Clear existing markers and routes
    clearMapData()

    if (!collectionPoints || collectionPoints.length === 0) {
      checkpointsList.innerHTML = '<p class="text-muted">No checkpoints found for this schedule.</p>'
      return
    }

    const binIcon = L.icon({
      iconUrl: "/img/elements/normalbin.png",
      iconSize: [30, 30],
      iconAnchor: [15, 30],
    })

    const waypoints = []
    checkpointsList.innerHTML = ""

    collectionPoints.forEach((point, index) => {
      // Add bin marker
      const marker = L.marker([point.latitude, point.longitude], { icon: binIcon })
        .addTo(map)
        .bindPopup(`
                    <div class="p-2">
                        <strong>Checkpoint ${point.orderInSchedule}</strong><br>
                        <strong>Bin:</strong> ${point.binPlateId}<br>
                        <strong>Location:</strong> ${point.location}<br>
                        <strong>Fill Level:</strong> ${point.fillLevel}%<br>
                        <strong>Status:</strong> ${point.isCollected ? "✅ Collected" : "⏳ Pending"}
                        ${point.collectedAt ? `<br><strong>Collected:</strong> ${new Date(point.collectedAt).toLocaleString()}` : ""}
                    </div>
                `)

      markers.push(marker)

      // Add numbered label
      const label = L.divIcon({
        className: "custom-div-icon",
        html: `<div style="background:${point.isCollected ? "#28a745" : "#6c5ce7"};color:white;border-radius:50%;width:24px;height:24px;line-height:24px;text-align:center;font-weight:bold">${point.orderInSchedule}</div>`,
        iconSize: [24, 24],
        iconAnchor: [12, 12],
      })

      const labelMarker = L.marker([point.latitude, point.longitude], { icon: label }).addTo(map)
      markers.push(labelMarker)

      waypoints.push(L.latLng(point.latitude, point.longitude))

      // Create checkpoint item
      const checkpointItem = createCheckpointItem(point)
      checkpointsList.appendChild(checkpointItem)
    })

    // Draw route line only (no instructions)
    if (waypoints.length > 1) {
      routingControl = L.Routing.control({
        waypoints: waypoints,
        lineOptions: {
          styles: [{ color: "#2ecc71", weight: 5, opacity: 0.9 }],
        },
        addWaypoints: false,
        draggableWaypoints: false,
        routeWhileDragging: false,
        fitSelectedRoutes: true,
        show: false, // This hides the instruction panel
        createMarker: () => null, // This prevents default markers
        router: new L.Routing.OSRMv1({
          serviceUrl: "https://router.project-osrm.org/route/v1",
        }),
      }).addTo(map)

      // Hide the routing instructions container completely
      const routingContainer = document.querySelector(".leaflet-routing-container")
      if (routingContainer) {
        routingContainer.style.display = "none"
      }
    }

    // Fit map to show all points
    if (markers.length > 0) {
      const group = new L.featureGroup(markers)
      map.fitBounds(group.getBounds().pad(0.1))
    }
  }

  function createCheckpointItem(point) {
    const item = document.createElement("div")
    item.className = `checkpoint-item ${point.isCollected ? "collected" : ""}`

    const fillClass = point.fillLevel <= 30 ? "fill-low" : point.fillLevel <= 70 ? "fill-medium" : "fill-high"

    item.innerHTML = `
            <div class="checkpoint-circle ${point.isCollected ? "collected" : ""}">
                ${point.isCollected ? "" : point.orderInSchedule}
            </div>
            <div class="checkpoint-details">
                <div class="checkpoint-title">${point.location}</div>
                <div class="checkpoint-subtitle">Bin: ${point.binPlateId} • Fill: ${point.fillLevel}%</div>
                ${point.collectedAt
        ? `<div class="checkpoint-time">Collected: ${new Date(point.collectedAt).toLocaleString()}</div>`
        : ""
      }
            </div>
            <div class="fill-indicator">
                <div class="fill-level ${fillClass}" style="height: ${point.fillLevel}%"></div>
            </div>
        `

    return item
  }

  function clearMapData() {
    // Remove existing markers
    markers.forEach((marker) => {
      if (map.hasLayer(marker)) {
        map.removeLayer(marker)
      }
    })
    markers = []

    // Remove existing routing control
    if (routingControl) {
      map.removeControl(routingControl)
      routingControl = null
    }
  }
})
