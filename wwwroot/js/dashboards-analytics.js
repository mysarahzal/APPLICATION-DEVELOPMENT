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

  document.addEventListener("DOMContentLoaded", function () {
    // Check if the map element exists before initializing
    if (document.querySelector('#map')) {
      var map = L.map('map').setView([1.55, 103.7], 11); // Centered roughly on Johor Bahru
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(map);

      // --- BIN DATA (Initial static examples - will be replaced with real data) ---
      const binsData = [
        {
          id: 1,
          bin_plate_id: 'BIN-JB-001',
          location_latitude: 1.565, // Example: Housing area 1
          location_longitude: 103.705,
          address: 'No. 10, Jalan Harmoni 1, Taman Harmoni, 81300 Skudai, Johor',
          type: 'residential',
          last_collected_at: '2025-06-03T10:00:00Z',
          status: 'collected', // collected, missed, pending
          client_id_image: 'https://via.placeholder.com/150/00FF00/FFFFFF?text=Collected+Image' // Green placeholder
        },
        {
          id: 2,
          bin_plate_id: 'BIN-JB-002',
          location_latitude: 1.54, // Example: Commercial area
          location_longitude: 103.73,
          address: 'Lot 2, Jalan Industri 5, Kawasan Perindustrian, 81100 Johor Bahru, Johor',
          type: 'commercial',
          last_collected_at: '2025-05-28T14:30:00Z',
          status: 'missed', // This one is older than 1 week, should be grey
          client_id_image: 'https://via.placeholder.com/150/FF0000/FFFFFF?text=Missed+Image' // Red placeholder
        },
        {
          id: 3,
          bin_plate_id: 'BIN-JB-003',
          location_latitude: 1.57, // Example: Another housing area
          location_longitude: 103.71,
          address: 'Apt. 3, Block C, Taman Perumahan Jaya, 81200 Johor Bahru, Johor',
          type: 'residential',
          last_collected_at: '2025-06-05T09:00:00Z',
          status: 'collected',
          client_id_image: 'https://via.placeholder.com/150/0000FF/FFFFFF?text=Another+Collected' // Blue placeholder
        },
        {
          id: 4,
          bin_plate_id: 'BIN-JB-004',
          location_latitude: 1.53,
          location_longitude: 103.72,
          address: 'No. 1, Jalan Bahagia, 80350 Johor Bahru, Johor',
          type: 'residential',
          last_collected_at: '2025-06-01T08:00:00Z', // More than 1 week
          status: 'collected', // This status will be overridden by the grey logic
          client_id_image: 'https://via.placeholder.com/150/FFFF00/000000?text=Old+Collected' // Yellow placeholder
        }
      ];

      // --- Helper function to determine bin color based on status and last collected date ---
      function getBinColor(bin) {
        const oneWeekAgo = new Date();
        oneWeekAgo.setDate(oneWeekAgo.getDate() - 7);
        const lastCollectedDate = new Date(bin.last_collected_at);

        if (bin.status === 'collected' && lastCollectedDate >= oneWeekAgo) {
          return 'green'; // Successfully collected within 1 week
        } else if (bin.status === 'missed') {
          return 'red'; // Explicitly missed
        } else if (lastCollectedDate < oneWeekAgo) {
          return 'grey'; // Not collected for 1 week or more
        }
        return 'blue'; // Default or other status
      }

      // --- Custom Bin Icons ---
      function createBinIcon(color) {
        let iconUrl;
        switch (color) {
          case 'green':
            iconUrl = 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-green.png';
            break;
          case 'red':
            iconUrl = 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png';
            break;
          case 'grey':
            iconUrl = 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-grey.png';
            break;
          case 'blue': // Default, could be for 'pending' or 'trucks' if you repurpose
          default:
            iconUrl = 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-blue.png';
            break;
        }
        return L.icon({
          iconUrl: iconUrl,
          shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
          iconSize: [25, 41],
          iconAnchor: [12, 41],
          popupAnchor: [1, -34],
          shadowSize: [41, 41]
        });
      }

      // --- Add Bins to Map ---
      binsData.forEach(bin => {
        const binColor = getBinColor(bin);
        const binIcon = createBinIcon(binColor);

        let popupContent = `
                <div style="font-family: sans-serif; font-size: 14px;">
                    <strong>Bin ID:</strong> ${bin.bin_plate_id}<br>
                    <strong>Address:</strong> ${bin.address}<br>
                    <strong>Type:</strong> ${bin.type}<br>
                    <strong>Last Collected:</strong> ${new Date(bin.last_collected_at).toLocaleString()}<br>
                    <strong>Status:</strong> <span style="color: ${binColor === 'green' ? 'green' : binColor === 'red' ? 'red' : 'grey'}; font-weight: bold;">${binColor === 'grey' ? 'Not Collected (>1 week)' : bin.status}</span><br>
                    ${bin.client_id_image ? `<br><img src="${bin.client_id_image}" alt="Last Collected Image" style="max-width: 150px; height: auto; border-radius: 5px;">` : ''}
                </div>
            `;
        L.marker([bin.location_latitude, bin.location_longitude], { icon: binIcon })
          .addTo(map)
          .bindPopup(popupContent);
      });

      // --- Truck Markers (simple 2D icons for now) ---
      // For 3D moving trucks, you'd need a more advanced library or a different approach (see notes below).
      const truckIcon = L.icon({
        iconUrl: 'https://cdn-icons-png.flaticon.com/512/7438/7438616.png', // A generic truck icon
        iconSize: [38, 38], // size of the icon
        iconAnchor: [19, 38], // point of the icon which will correspond to marker's location
        popupAnchor: [0, -38] // point from which the popup should open relative to the iconAnchor
      });

      const trucksData = [
        { id: 101, name: 'Truck A (JSB7000)', lat: 1.552, lng: 103.68, status: 'moving', last_updated: '2025-06-06T01:00:00Z' },
        { id: 102, name: 'Truck B (JSD1234)', lat: 1.57, lng: 103.69, status: 'idle', last_updated: '2025-06-06T01:10:00Z' },
        { id: 103, name: 'Truck C (JST5678)', lat: 1.53, lng: 103.71, status: 'on_route', last_updated: '2025-06-06T01:15:00Z' },
        { id: 104, name: 'Truck D (JSF9012)', lat: 1.56, lng: 103.725, status: 'loading', last_updated: '2025-06-06T01:20:00Z' }
      ];

      trucksData.forEach(truck => {
        L.marker([truck.lat, truck.lng], { icon: truckIcon })
          .addTo(map)
          .bindPopup(`
                    <strong>${truck.name}</strong><br>
                    Status: ${truck.status}<br>
                    Last Updated: ${new Date(truck.last_updated).toLocaleString()}
                `);
      });

      // --- Placeholder for Routes (can be Polyline or Polygon) ---
      // Example: A simple route line
      const routeCoordinates = [
        [1.54, 103.69],
        [1.55, 103.70],
        [1.56, 103.71],
        [1.555, 103.72]
      ];
      L.polyline(routeCoordinates, { color: 'purple', weight: 4, opacity: 0.7 }).addTo(map)
        .bindPopup("Example Route 1");
    }
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
