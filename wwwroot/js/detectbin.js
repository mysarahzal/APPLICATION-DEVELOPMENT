document.addEventListener("DOMContentLoaded", function () {
  const cameraContainer = document.getElementById('cameraContainer');
  const video = document.getElementById('videoElement');
  const captureBtn = document.getElementById('captureBtn');
  const startCameraBtn = document.getElementById('startCameraBtn');

  let stream;

  startCameraBtn.addEventListener('click', async () => {
    cameraContainer.style.display = 'block';
    startCameraBtn.style.display = 'none';

    try {
      stream = await navigator.mediaDevices.getUserMedia({ video: true });
      video.srcObject = stream;
    } catch (err) {
      alert("⚠️ Could not access the camera: " + err.message);
    }
  });

  captureBtn.addEventListener('click', async () => {
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const context = canvas.getContext('2d');
    context.drawImage(video, 0, 0, canvas.width, canvas.height);
    const base64Image = canvas.toDataURL('image/png');

    // Send image to backend
    const response = await fetch('/BinDetector/DetectBin', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: `base64Image=${encodeURIComponent(base64Image)}`
    });

    const result = await response.json();

    if (stream) {
      stream.getTracks().forEach(track => track.stop());
      video.srcObject = null;
    }

    if (result.success) {
      alert(`✅ Bin Plate Detected: ${result.plate}`);
    } else {
      alert(`❌ ${result.message}`);
    }

    cameraContainer.style.display = 'none';
    startCameraBtn.style.display = 'block';
  });
});
