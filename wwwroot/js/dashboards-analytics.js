// Declare necessary variables
let config, ApexCharts, L, html2canvas

// Import necessary libraries
config = window.config || {} // Assuming config is available globally
ApexCharts = window.ApexCharts // Assuming ApexCharts is available globally
L = window.L // Assuming Leaflet is available globally
html2canvas = window.html2canvas // Assuming html2canvas is available globally
  ; (() => {
    let cardColor, labelColor, borderColor, chartBgColor, bodyColor

    cardColor = config.colors.cardColor
    labelColor = config.colors.textMuted
    borderColor = config.colors.borderColor
    chartBgColor = config.colors.chartBgColor
    bodyColor = config.colors.bodyColor

    // Pickup over Time - Area Chart (Total Bin Pickup with Missed Pickup)
    document.addEventListener("DOMContentLoaded", () => {
      const totalMissedPickupsAreaOptions = {
        series: [
          {
            name: "Total Bin Pickups",
            data: window.totalPickupsByDay, // from Razor
          },
          {
            name: "Missed Pickups",
            data: window.missedPickupsByDay,
          },
        ],
        chart: {
          height: 300,
          type: "area",
          toolbar: {
            show: true,
            tools: { download: true },
          },
        },
        dataLabels: { enabled: false },
        stroke: { curve: "smooth" },
        xaxis: {
          categories: window.pickupDays, // ["Mon", ..., "Sat"]
        },
        colors: ["#00cfe8", "#EA5455"],
        tooltip: { x: {} },
        legend: {
          position: "top",
          horizontalAlign: "left",
        },
      };

      new ApexCharts(document.querySelector("#pickupAreaChart"), totalMissedPickupsAreaOptions).render();
    });

    // Pickup by Collector - Bar Chart (Total Bins Collected and Missed Bins)
    document.addEventListener("DOMContentLoaded", () => {
      // --------------------------------------------------------
      // Bar: Pickups vs Missed per Collector
      // --------------------------------------------------------
      const pickupByCollectorBarOptions = {
        series: [
          { name: "Total Bins Collected", data: window.collectedSeries },
          { name: "Missed Bins", data: window.missedSeries },
        ],
        chart: {
          type: "bar",
          height: 300,
          toolbar: { show: true, tools: { download: true } },
        },
        plotOptions: {
          bar: { horizontal: false, columnWidth: "55%", endingShape: "rounded" },
        },
        dataLabels: { enabled: false },
        stroke: { show: true, width: 2, colors: ["transparent"] },
        xaxis: { categories: window.collectorCategories },
        yaxis: {
          title: { text: "Number of Bins", style: { fontWeight: "bold", fontSize: "14px", color: "#000" } },
        },
        fill: { opacity: 1 },
        colors: ["#7367f0", "#ff9f43"],
        tooltip: { y: { formatter: (val) => `${val} bins` } },
        legend: { position: "top", horizontalAlign: "left" },
      };

      const target = document.querySelector("#pickupByCollectorBarChart");
      if (target) new ApexCharts(target, pickupByCollectorBarOptions).render();
    });


    // ALL ROUTES MAP - Enhanced with Real Database Data
    document.addEventListener("DOMContentLoaded", () => {
      if (!document.querySelector("#map")) return

      const map = L.map("map").setView([1.56, 103.72], 12)

      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: "&copy; OpenStreetMap contributors",
      }).addTo(map)

      // Custom icons
      const binIcons = {
        collected: L.icon({ iconUrl: "/img/elements/greenbin.png", iconSize: [25, 25], iconAnchor: [12, 25] }),
        pending: L.icon({ iconUrl: "/img/elements/normalbin.png", iconSize: [25, 25], iconAnchor: [12, 25] }),
      }

      // Get routes data from the view
      const allRoutesData = window.allRoutesData || []
      const routeControls = []
      const routeLegend = document.getElementById("routeLegend")

      // Clear legend
      if (routeLegend) {
        routeLegend.innerHTML = ""
      }

      // Process each route
      allRoutesData.forEach((route, index) => {
        if (route.collectionPoints && route.collectionPoints.length > 0) {
          // Add collection points for this route
          route.collectionPoints.forEach((point, pointIndex) => {
            const icon = point.isCollected ? binIcons.collected : binIcons.pending

            const marker = L.marker([point.latitude, point.longitude], { icon })
              .addTo(map)
              .bindPopup(`
              <div class="p-2">
                <strong>${route.truckName} - ${route.routeName}</strong><br>
                <strong>Checkpoint ${point.orderInSchedule}</strong><br>
                <strong>Bin:</strong> ${point.binPlateId}<br>
                <strong>Location:</strong> ${point.location}<br>
                <strong>Fill Level:</strong> ${point.fillLevel}%<br>
                <strong>Status:</strong> ${point.isCollected ? "✅ Collected" : "⏳ Pending"}
                ${point.collectedAt ? `<br><strong>Collected:</strong> ${new Date(point.collectedAt).toLocaleString()}` : ""}
              </div>
            `)

            // Add numbered label
            const label = L.divIcon({
              className: "custom-div-icon",
              html: `<div style="background:${route.routeColor};color:white;border-radius:50%;width:20px;height:20px;line-height:20px;text-align:center;font-weight:bold;font-size:11px">${point.orderInSchedule}</div>`,
              iconSize: [20, 20],
              iconAnchor: [10, 10],
            })

            L.marker([point.latitude, point.longitude], { icon: label }).addTo(map)
          })

          // Create route waypoints
          const waypoints = route.collectionPoints.map((point) => L.latLng(point.latitude, point.longitude))

          // Draw route line
          if (waypoints.length > 1) {
            const routeControl = L.Routing.control({
              waypoints: waypoints,
              lineOptions: {
                styles: [{ color: route.routeColor, weight: 4, opacity: 0.8 }],
              },
              addWaypoints: false,
              draggableWaypoints: false,
              routeWhileDragging: false,
              fitSelectedRoutes: false,
              show: false,
              createMarker: () => null,
              router: new L.Routing.OSRMv1({
                serviceUrl: "https://router.project-osrm.org/route/v1",
              }),
            }).addTo(map)

            routeControls.push({ control: routeControl, route: route })

            // Hide routing instructions
            setTimeout(() => {
              const routingContainer = document.querySelector(".leaflet-routing-container")
              if (routingContainer) {
                routingContainer.style.display = "none"
              }
            }, 100)
          }

          // Add to legend
          if (routeLegend) {
            const legendItem = document.createElement("div")
            legendItem.className = "route-legend-item"
            legendItem.innerHTML = `
            <div class="route-color-box" style="background-color: ${route.routeColor}"></div>
            <div>
              <strong>${route.truckName}</strong><br>
              <small class="text-muted">${route.routeName} (${route.collectionPoints.length} points)</small>
            </div>
            <div class="route-toggle">
              <input type="checkbox" checked data-route-index="${index}" class="form-check-input">
            </div>
          `
            routeLegend.appendChild(legendItem)
          }
        }
      })

      // Handle route toggle
      if (routeLegend) {
        routeLegend.addEventListener("change", (e) => {
          if (e.target.type === "checkbox") {
            const routeIndex = Number.parseInt(e.target.dataset.routeIndex)
            const routeControl = routeControls[routeIndex]

            if (routeControl) {
              if (e.target.checked) {
                routeControl.control.addTo(map)
              } else {
                map.removeControl(routeControl.control)
              }
            }
          }
        })
      }

      // Fit map to show all routes
      if (allRoutesData.length > 0) {
        const allPoints = allRoutesData.flatMap((route) =>
          route.collectionPoints.map((point) => [point.latitude, point.longitude]),
        )

        if (allPoints.length > 0) {
          const group = new L.featureGroup(allPoints.map((point) => L.marker(point)))
          map.fitBounds(group.getBounds().pad(0.1))
        }
      }
    })

    // DownloadPDF
    document.addEventListener("DOMContentLoaded", () => {
      const downloadPdfButton = document.getElementById("downloadDashboardPdf")
      if (downloadPdfButton) {
        downloadPdfButton.addEventListener("click", () => {
          const dashboardContent = document.getElementById("dashboard-printable-area")
          if (dashboardContent) {
            html2canvas(dashboardContent, {
              scale: 2,
              useCORS: true,
              logging: false,
            })
              .then((canvas) => {
                const imgData = canvas.toDataURL("image/png")
                const { jsPDF } = window.jspdf
                const pdf = new jsPDF("p", "mm", "a4")
                const imgWidth = 210
                const pageHeight = 297
                const imgHeight = (canvas.height * imgWidth) / canvas.width
                let heightLeft = imgHeight
                let position = 0

                pdf.addImage(imgData, "PNG", 0, position, imgWidth, imgHeight)
                heightLeft -= pageHeight

                while (heightLeft > 0) {
                  position = heightLeft - imgHeight
                  pdf.addPage()
                  pdf.addImage(imgData, "PNG", 0, position, imgWidth, imgHeight)
                  heightLeft -= pageHeight
                }

                pdf.save("dashboard-report.pdf")
              })
              .catch((error) => {
                console.error("Error generating PDF:", error)
                alert("Could not generate PDF. Please ensure all content is loaded and try again.")
              })
          }
        })
      }
    })
  })()
