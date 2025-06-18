/**
 * Dashboard Analytics
 */

'use strict';

(function () {
  let cardColor, labelColor, borderColor, chartBgColor, bodyColor;

  cardColor = config.colors.cardColor;
  labelColor = config.colors.textMuted;
  borderColor = config.colors.borderColor;
  chartBgColor = config.colors.chartBgColor;
  bodyColor = config.colors.bodyColor;

  // Pickup over Time - Area Chart (Total Bin Pickup with Missed Pickup)
  document.addEventListener("DOMContentLoaded", function () {
    var totalMissedPickupsAreaOptions = {
      series: [
        {
          name: 'Total Bin Pickups',
          data: [10, 15, 12, 18, 22, 19, 25] // Calculated total pickups
        },
        {
          name: 'Missed Pickups',
          data: [1, 1, 1, 1, 1, 1, 2] // Missed pickups data
        }
      ],
      chart: {
        height: 300,
        type: 'area', // Changed to area chart
        toolbar: {
          show: true,
          tools: {
            download: true
          }
        }
      },
      dataLabels: {
        enabled: false
      },
      stroke: {
        curve: 'smooth'
      },
      xaxis: {
        categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
      },
      colors: ['#00cfe8', '#EA5455'], // Example colors for Total and Missed
      tooltip: {
        x: {}
      },
      legend: {
        position: 'top',
        horizontalAlign: 'left'
      }
    };

    if (document.querySelector("#totalMissedPickupsAreaChart")) {
      var totalMissedPickupsAreaChart = new ApexCharts(document.querySelector("#totalMissedPickupsAreaChart"), totalMissedPickupsAreaOptions);
      totalMissedPickupsAreaChart.render();
    }
  });


  // Pickup by Collector - Bar Chart (Total Bins Collected and Missed Bins)
  document.addEventListener("DOMContentLoaded", function () {
    var pickupByCollectorBarOptions = {
      series: [
        {
          name: 'Total Bins Collected',
          data: [120, 98, 135, 110]
        },
        {
          name: 'Missed Bins',
          data: [5, 3, 7, 4] // Sample missed bins data
        }
      ],
      chart: {
        type: 'bar',
        height: 300,
        toolbar: {
          show: true,
          tools: {
            download: true
          }
        }
      },
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '55%',
          endingShape: 'rounded'
        },
      },
      dataLabels: {
        enabled: false
      },
      stroke: {
        show: true,
        width: 2,
        colors: ['transparent']
      },
      xaxis: {
        categories: ['Ahmad', 'Ravi', 'Nina', 'Fahmi'],
      },
      yaxis: {
        title: {
          text: 'Number of Bins',
          style: {
            fontWeight: 'bold',
            fontSize: '14px', // <--- You can try adjusting this to see if it helps clarity
            color: '#000' // <--- Explicitly set color to black for sharpness
          }
        }
      },
      fill: {
        opacity: 1
      },
      colors: ['#7367f0', '#ff9f43'], // Example colors for Total and Missed
      tooltip: {
        y: {
          formatter: function (val) {
            return val + " bins"
          }
        }
      },
      legend: {
        position: 'top',
        horizontalAlign: 'left'
      }
    };

    if (document.querySelector("#pickupByCollectorBarChart")) {
      var pickupByCollectorBarChart = new ApexCharts(document.querySelector("#pickupByCollectorBarChart"), pickupByCollectorBarOptions);
      pickupByCollectorBarChart.render();
    }
  });

  // dashboards-analytics.js

  // Bin Map - Thevan Improved
  document.addEventListener("DOMContentLoaded", function () {
    if (!document.querySelector('#map')) return;

    const map = L.map('map').setView([1.56, 103.72], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // 🔰 Custom icons
    const binIcons = {
      green: L.icon({ iconUrl: '/img/elements/greenbin.png', iconSize: [30, 30], iconAnchor: [15, 30] }),
      red: L.icon({ iconUrl: '/img/elements/redbin.png', iconSize: [30, 30], iconAnchor: [15, 30] }),
      grey: L.icon({ iconUrl: '/img/elements/normalbin.png', iconSize: [30, 30], iconAnchor: [15, 30] })
    };

    const truckIcon = L.icon({
      iconUrl: '/img/elements/3d-truck.png',
      iconSize: [48, 48],
      iconAnchor: [24, 48],
      popupAnchor: [0, -48]
    });

    //  Bin Data with realistic statuses
    const binsData = [
      { plate: 'BIN001', lat: 1.565, lng: 103.705, status: 'collected', address: 'Taman Harmoni', collected_at: '2025-06-18T08:00:00Z' },
      { plate: 'BIN002', lat: 1.543, lng: 103.735, status: 'missed', address: 'Perindustrian', collected_at: null },
      { plate: 'BIN003', lat: 1.575, lng: 103.715, status: 'pending', address: 'Taman Perumahan Jaya', collected_at: null },
      { plate: 'BIN004', lat: 1.552, lng: 103.69, status: 'collected', address: 'Jalan Bahagia', collected_at: '2025-06-18T07:30:00Z' },
      { plate: 'BIN005', lat: 1.558, lng: 103.726, status: 'pending', address: 'Larkin Sentral', collected_at: null },
      { plate: 'BIN006', lat: 1.547, lng: 103.74, status: 'missed', address: 'Taman Tampoi', collected_at: null }
    ];

    //  Add bins and build route
    const routeWaypoints = [];
    binsData.forEach(bin => {
      let icon;
      if (bin.status === 'collected') {
        icon = binIcons.green;
      } else if (bin.status === 'missed') {
        icon = binIcons.red;
      } else {
        icon = binIcons.grey;
      }

      L.marker([bin.lat, bin.lng], { icon })
        .addTo(map)
        .bindPopup(`
        <b>${bin.plate}</b><br>
        ${bin.address}<br>
        Status: <strong style="color:${bin.status === 'missed' ? 'red' : bin.status === 'collected' ? 'green' : 'gray'}">${bin.status}</strong>
      `);

      routeWaypoints.push(L.latLng(bin.lat, bin.lng));
    });

    // 🚚 Truck Marker
    const truckMarker = L.marker(routeWaypoints[0], { icon: truckIcon }).addTo(map).bindPopup("🚛 Truck starting route");

    // 🔃 Route animation using Leaflet Routing Machine
    L.Routing.control({
      waypoints: routeWaypoints,
      lineOptions: {
        styles: [{ color: '#6f42c1', weight: 5, opacity: 0.8 }]
      },
      addWaypoints: false,
      draggableWaypoints: false,
      routeWhileDragging: false,
      fitSelectedRoutes: true,
      show: false, // ❌ Hide instruction box
      createMarker: () => null, // ❌ Suppress default markers
      router: new L.Routing.OSRMv1({
        serviceUrl: 'https://router.project-osrm.org/route/v1'
      })
    })
      .on('routesfound', function (e) {
        const route = e.routes[0];
        const coordinates = route.coordinates;
        let index = 0;

        function animateTruck() {
          if (index < coordinates.length) {
            const latlng = L.latLng(coordinates[index].lat, coordinates[index].lng);
            truckMarker.setLatLng(latlng);
            index++;
            setTimeout(animateTruck, 800); // 🕐 Adjust speed (lower = faster)
          } else {
            truckMarker.bindPopup("✅ Truck reached final destination").openPopup();
          }
        }

        animateTruck(); // 🎬 Start animation
      })
      .addTo(map);
  });

  //DownloadPDF
  document.addEventListener("DOMContentLoaded", function () {
    const downloadPdfButton = document.getElementById('downloadDashboardPdf');

    if (downloadPdfButton) {
      downloadPdfButton.addEventListener('click', function () {
        // Get the specific element you want to convert to PDF
        const dashboardContent = document.getElementById('dashboard-printable-area');

        if (dashboardContent) {
          // Temporarily hide elements that shouldn't be in the PDF
          // (e.g., if you have elements that appear *within* the printable area
          // but should not be in the PDF, like specific buttons on cards)
          // Example: const elementsToHide = dashboardContent.querySelectorAll('.no-print');
          // elementsToHide.forEach(el => el.style.display = 'none');

          html2canvas(dashboardContent, {
            scale: 2, // Increase scale for better image quality in the PDF
            useCORS: true, // Important if your dashboard includes images from other domains
            logging: false // Disable logging if you don't want html2canvas messages in console
          }).then(canvas => {
            const imgData = canvas.toDataURL('image/png');
            const { jsPDF } = window.jspdf; // Get jsPDF from the global window object

            // PDF dimensions (A4 size, portrait)
            const pdf = new jsPDF('p', 'mm', 'a4');
            const imgWidth = 210; // A4 width in mm
            const pageHeight = 297; // A4 height in mm

            // Calculate the image height to maintain aspect ratio
            const imgHeight = canvas.height * imgWidth / canvas.width;

            let heightLeft = imgHeight;
            let position = 0;

            // Add the first page
            pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
            heightLeft -= pageHeight;

            // If content overflows one page, add new pages
            while (heightLeft > 0) {
              position = heightLeft - imgHeight; // Calculate new position for the next page
              pdf.addPage();
              pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
              heightLeft -= pageHeight;
            }

            pdf.save('dashboard-report.pdf');

            // Restore hidden elements after PDF generation
            // elementsToHide.forEach(el => el.style.display = '');
          }).catch(error => {
            console.error("Error generating PDF:", error);
            alert("Could not generate PDF. Please ensure all content is loaded and try again.");
          });
        } else {
          console.error("Dashboard printable area element not found!");
        }
      });
    }
  });
})();
