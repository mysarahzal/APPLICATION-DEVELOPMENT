// Global variables
let model, tesseractWorker, video, canvas, ctx
let isModelLoaded = false
let isOCRReady = false
let liveDetectionCount = 0
let isProcessing = false
let lastDetectedPlate = null
let lastDetectionTime = 0

// Manual entry variables
let manualVideo, manualCanvas, manualCtx
let capturedImageData = null
let isManualCameraActive = false

// GPS location
const currentLocation = { latitude: 0, longitude: 0 }

// Current mode
let currentMode = "auto" // 'auto' or 'manual'

// Function to stop manual camera
function stopManualCamera() {
  if (manualVideo && manualVideo.srcObject) {
    manualVideo.srcObject.getTracks().forEach((track) => track.stop())
  }
  isManualCameraActive = false
}

// Auto-load everything when page loads
document.addEventListener("DOMContentLoaded", async () => {
  setupModeToggle()
  await getCurrentLocation()

  // Start with auto mode
  await initializeAutoMode()

  // Setup manual mode handlers
  setupManualMode()
})

function setupModeToggle() {
  console.log("🔧 Setting up mode toggle...")

  const autoModeBtn = document.getElementById("autoModeBtn")
  const manualModeBtn = document.getElementById("manualModeBtn")
  const autoSection = document.getElementById("autoDetectionSection")
  const manualSection = document.getElementById("manualEntrySection")

  // Debug: Check if elements exist
  console.log("Auto button:", autoModeBtn)
  console.log("Manual button:", manualModeBtn)
  console.log("Auto section:", autoSection)
  console.log("Manual section:", manualSection)

  if (!autoModeBtn || !manualModeBtn || !autoSection || !manualSection) {
    console.error("❌ Missing required elements for mode toggle!")
    return
  }

  autoModeBtn.addEventListener("click", async () => {
    console.log("🔄 Auto mode clicked")
    if (currentMode === "auto") return

    currentMode = "auto"
    autoModeBtn.classList.add("active")
    autoModeBtn.classList.remove("btn-outline-primary")
    autoModeBtn.classList.add("btn-primary")

    manualModeBtn.classList.remove("active")
    manualModeBtn.classList.add("btn-outline-primary")
    manualModeBtn.classList.remove("btn-primary")

    autoSection.style.display = "block"
    manualSection.style.display = "none"

    // Stop manual camera if active
    stopManualCamera()

    // Initialize auto mode if not already done
    if (!isModelLoaded || !isOCRReady) {
      await initializeAutoMode()
    }
  })

  manualModeBtn.addEventListener("click", () => {
    console.log("🔄 Manual mode clicked")
    if (currentMode === "manual") return

    currentMode = "manual"
    manualModeBtn.classList.add("active")
    manualModeBtn.classList.remove("btn-outline-primary")
    manualModeBtn.classList.add("btn-primary")

    autoModeBtn.classList.remove("active")
    autoModeBtn.classList.add("btn-outline-primary")
    autoModeBtn.classList.remove("btn-primary")

    autoSection.style.display = "none"
    manualSection.style.display = "block"

    console.log("✅ Manual section should now be visible")

    // Update GPS display for manual mode
    updateManualGPSDisplay()
  })

  console.log("✅ Mode toggle setup complete")
}

async function initializeAutoMode() {
  await loadDetectionModel()
  await loadOCR()
  await startCamera()
}

function setupManualMode() {
  console.log("🔧 Setting up manual mode...")

  const manualForm = document.getElementById("manualEntryForm")
  const plateInput = document.getElementById("manualPlateId")
  const validateBtn = document.getElementById("validatePlateBtn")
  const photoUpload = document.getElementById("photoUpload")
  const refreshLocationBtn = document.getElementById("refreshLocationBtn")
  const submitBtn = document.getElementById("submitManualBtn")

  // Debug: Check if elements exist
  console.log("Manual form:", manualForm)
  console.log("Plate input:", plateInput)
  console.log("Validate button:", validateBtn)
  console.log("Photo upload:", photoUpload)
  console.log("Refresh location button:", refreshLocationBtn)
  console.log("Submit button:", submitBtn)

  if (!manualForm || !plateInput || !validateBtn || !photoUpload || !refreshLocationBtn || !submitBtn) {
    console.error("❌ Missing required elements for manual mode!")
    return
  }

  // Plate ID validation
  plateInput.addEventListener("input", (e) => {
    e.target.value = e.target.value.toUpperCase()
    validateManualForm()
  })

  validateBtn.addEventListener("click", async () => {
    await validateBinPlate()
  })

  // Photo upload handling
  photoUpload.addEventListener("change", (e) => {
    handlePhotoUpload(e)
  })

  // GPS refresh
  refreshLocationBtn.addEventListener("click", async () => {
    await getCurrentLocation()
    updateManualGPSDisplay()
  })

  // Form submission
  manualForm.addEventListener("submit", async (e) => {
    e.preventDefault()
    await submitManualEntry()
  })
}

function handlePhotoUpload(event) {
  const file = event.target.files[0]
  if (!file) {
    capturedImageData = null
    document.getElementById("uploadedImagePreview").style.display = "none"
    validateManualForm()
    return
  }

  // Validate file type
  if (!file.type.startsWith("image/")) {
    showResult("❌ Please select a valid image file", "error")
    event.target.value = ""
    return
  }

  // Validate file size (max 5MB)
  if (file.size > 5 * 1024 * 1024) {
    showResult("❌ Image file too large. Please select a file under 5MB", "error")
    event.target.value = ""
    return
  }

  const reader = new FileReader()
  reader.onload = (e) => {
    capturedImageData = e.target.result

    // Show preview
    const previewImg = document.getElementById("previewUploadedImage")
    const previewDiv = document.getElementById("uploadedImagePreview")

    previewImg.src = capturedImageData
    previewDiv.style.display = "block"

    validateManualForm()
  }

  reader.readAsDataURL(file)
}

async function validateBinPlate() {
  const plateId = document.getElementById("manualPlateId").value.trim()
  const validationDiv = document.getElementById("plateValidation")
  const validateBtn = document.getElementById("validatePlateBtn")

  if (!plateId) {
    validationDiv.innerHTML = '<small class="text-warning">Please enter a plate ID</small>'
    return
  }

  validateBtn.disabled = true
  validateBtn.innerHTML = "⏳ Checking..."

  try {
    const response = await fetch("/Scan/ValidateBinPlate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ binPlateId: plateId }),
    })

    const result = await response.json()

    if (result.valid) {
      validationDiv.innerHTML = `
        <div class="alert alert-success alert-sm p-2">
          <small><strong>${result.message}</strong></small>
          ${result.binInfo
          ? `
            <br><small>📍 ${result.binInfo.location || "Unknown location"}</small>
            <br><small>🏢 ${result.binInfo.clientName}</small>
          `
          : ""
        }
        </div>
      `
    } else {
      const alertClass = result.alreadyCollected ? "alert-warning" : "alert-danger"
      validationDiv.innerHTML = `
        <div class="alert ${alertClass} alert-sm p-2">
          <small><strong>${result.message}</strong></small>
        </div>
      `
    }
  } catch (error) {
    validationDiv.innerHTML = `
      <div class="alert alert-danger alert-sm p-2">
        <small><strong>Validation failed: ${error.message}</strong></small>
      </div>
    `
  } finally {
    validateBtn.disabled = false
    validateBtn.innerHTML = "🔍 Validate"
    validateManualForm()
  }
}

function validateManualForm() {
  const plateId = document.getElementById("manualPlateId").value.trim()
  const submitBtn = document.getElementById("submitManualBtn")

  const hasValidPlate = plateId.length === 7 && /^[A-Z]{3}\d{4}$/.test(plateId)
  const hasPhoto = capturedImageData !== null
  const hasLocation = currentLocation.latitude !== 0 && currentLocation.longitude !== 0

  submitBtn.disabled = !(hasValidPlate && hasPhoto && hasLocation)

  if (submitBtn.disabled) {
    const missing = []
    if (!hasValidPlate) missing.push("Valid plate ID")
    if (!hasPhoto) missing.push("Photo upload")
    if (!hasLocation) missing.push("GPS location")

    submitBtn.innerHTML = `⚠️ Missing: ${missing.join(", ")}`
  } else {
    submitBtn.innerHTML = "✅ Submit Manual Entry"
  }
}

async function submitManualEntry() {
  const plateId = document.getElementById("manualPlateId").value.trim()
  const submitBtn = document.getElementById("submitManualBtn")

  if (!capturedImageData) {
    showResult("❌ Please capture a photo first", "error")
    return
  }

  submitBtn.disabled = true
  submitBtn.innerHTML = "⏳ Submitting..."

  try {
    showResult(
      `
      <div class="alert alert-info">
        📝 Manual entry: <strong>${plateId}</strong><br>
        📤 Processing and saving to database...
        ${currentLocation.latitude ? `<br>📍 GPS: ${currentLocation.latitude.toFixed(6)}, ${currentLocation.longitude.toFixed(6)}` : ""}
      </div>
    `,
      "info",
    )

    const response = await fetch("/Scan/ProcessManualEntry", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        binPlateId: plateId,
        imageData: capturedImageData,
        latitude: currentLocation.latitude || 0,
        longitude: currentLocation.longitude || 0,
      }),
    })

    const result = await response.json()

    if (result.success) {
      if (result.alreadyCollected) {
        showResult(
          `
          <div class="alert alert-warning">
            ⚠️ ${result.message}<br>
            <small>Plate: <strong>${result.binPlateId}</strong> - Last collected: ${result.collectionTime}</small>
          </div>
        `,
          "warning",
        )
      } else {
        showResult(
          `
          <div class="alert alert-success">
            <h5>✅ ${result.message}</h5>
            <hr>
            <div class="row">
              <div class="col-md-6">
                <p><strong>📝 Entry Method:</strong> ${result.detectionMethod}</p>
                <p><strong>🔍 Plate ID:</strong> ${result.binPlateId}</p>
                <p><strong>📍 Location:</strong> ${result.binLocation || "Not specified"}</p>
                <p><strong>📅 Collection Time:</strong> ${result.collectionTime}</p>
              </div>
              <div class="col-md-6">
                <p><strong>🏢 Client:</strong> ${result.clientName || "Not specified"}</p>
                <p><strong>👤 Collector:</strong> ${result.collectorName || "Unknown"}</p>
                <p><strong>🚛 Truck:</strong> ${result.truckLicensePlate || "Unknown"}</p>
                <p><strong>🌍 GPS:</strong> ${result.gpsLocation || "Not available"}</p>
              </div>
            </div>
          </div>
        `,
          "success",
        )

        // Reset form after successful submission
        resetManualForm()
      }
    } else {
      showResult(
        `
        <div class="alert alert-warning">
          ⚠️ ${result.message}
          <br><small>Entered plate: <strong>${plateId}</strong></small>
        </div>
      `,
        "warning",
      )
    }
  } catch (error) {
    console.error("Manual submission error:", error)
    showResult(
      `
      <div class="alert alert-danger">
        ❌ Submission failed: ${error.message}
        <br><small>Entered plate: <strong>${plateId}</strong></small>
      </div>
    `,
      "error",
    )
  } finally {
    submitBtn.disabled = false
    submitBtn.innerHTML = "✅ Submit Manual Entry"
  }
}

function resetManualForm() {
  document.getElementById("manualPlateId").value = ""
  document.getElementById("plateValidation").innerHTML = ""
  document.getElementById("photoUpload").value = ""
  document.getElementById("uploadedImagePreview").style.display = "none"
  capturedImageData = null
  validateManualForm()
}

function updateManualGPSDisplay() {
  const gpsInput = document.getElementById("manualGpsDisplay")
  if (gpsInput) {
    if (currentLocation.latitude && currentLocation.longitude) {
      gpsInput.value = `${currentLocation.latitude.toFixed(6)}, ${currentLocation.longitude.toFixed(6)}`
      gpsInput.className = "form-control text-success"
    } else {
      gpsInput.value = "Location not available"
      gpsInput.className = "form-control text-warning"
    }
  }
  validateManualForm()
}

// [Keep all existing auto-detection functions unchanged]
async function loadDetectionModel() {
  try {
    updateStatus("📦 Loading TensorFlow.js...")
    await loadScript("https://cdn.jsdelivr.net/npm/@tensorflow/tfjs@4.10.0/dist/tf.min.js")
    updateStatus("📦 Loading object detection model...")
    await loadScript("https://cdn.jsdelivr.net/npm/@tensorflow-models/coco-ssd@2.2.2/dist/coco-ssd.min.js")
    window.cocoSsd = window.cocoSsd || {}
    window.cocoSsd.load = window.cocoSsd.load || (() => Promise.resolve({}))
    model = await window.cocoSsd.load()
    isModelLoaded = true
    updateStatus("✅ Detection model loaded successfully!")
  } catch (error) {
    console.error("Detection model loading failed:", error)
    updateStatus("❌ Detection model failed: " + error.message)
  }
}

async function loadOCR() {
  try {
    updateStatus("📦 Loading OCR engine for ABC1234 format...")
    await loadScript("https://cdn.jsdelivr.net/npm/tesseract.js@4.1.1/dist/tesseract.min.js")
    window.Tesseract = window.Tesseract || {}
    window.Tesseract.createWorker = window.Tesseract.createWorker || (() => Promise.resolve({}))
    window.Tesseract.PSM = window.Tesseract.PSM || { SINGLE_BLOCK: 0 }
    window.Tesseract.OEM = window.Tesseract.OEM || { LSTM_ONLY: 0 }
    tesseractWorker = await window.Tesseract.createWorker({
      logger: (m) => {
        if (m.status === "loading tesseract core") {
          updateStatus("📦 Loading OCR core...")
        } else if (m.status === "loading language traineddata") {
          updateStatus("📚 Loading language data...")
        }
      },
    })
    await tesseractWorker.loadLanguage("eng")
    await tesseractWorker.initialize("eng")
    await tesseractWorker.setParameters({
      tessedit_char_whitelist: "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",
      tessedit_pageseg_mode: window.Tesseract.PSM.SINGLE_BLOCK,
      tessedit_ocr_engine_mode: window.Tesseract.OEM.LSTM_ONLY,
      preserve_interword_spaces: "0",
      user_defined_dpi: "300",
    })
    isOCRReady = true
    updateStatus("✅ Auto-detection ready! Point camera at ABC1234 number plate.")
    setTimeout(() => {
      document.getElementById("status").style.display = "none"
      document.getElementById("cameraSection").style.display = "block"
      startAutoDetection()
    }, 2000)
  } catch (error) {
    console.error("OCR loading failed:", error)
    updateStatus("❌ OCR failed: " + error.message)
  }
}

async function getCurrentLocation() {
  return new Promise((resolve) => {
    if (!navigator.geolocation) {
      console.warn("Geolocation not supported")
      resolve()
      return
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        currentLocation.latitude = position.coords.latitude
        currentLocation.longitude = position.coords.longitude
        console.log("✅ Location obtained:", currentLocation)
        updateGPSDisplay()
        updateManualGPSDisplay()
        resolve()
      },
      (error) => {
        console.warn("Location access denied:", error)
        updateGPSDisplay("Location access denied")
        updateManualGPSDisplay()
        resolve() // Continue without location
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 300000 },
    )
  })
}

function updateGPSDisplay(errorMessage = null) {
  const gpsElement = document.getElementById("gpsLocation")
  if (gpsElement) {
    if (errorMessage) {
      gpsElement.textContent = errorMessage
      gpsElement.className = "text-warning"
    } else if (currentLocation.latitude && currentLocation.longitude) {
      gpsElement.textContent = `${currentLocation.latitude.toFixed(6)}, ${currentLocation.longitude.toFixed(6)}`
      gpsElement.className = "text-success"
    } else {
      gpsElement.textContent = "Getting location..."
      gpsElement.className = "text-muted"
    }
  }
}

async function loadScript(src) {
  return new Promise((resolve, reject) => {
    const script = document.createElement("script")
    script.src = src
    script.onload = resolve
    script.onerror = reject
    document.head.appendChild(script)
  })
}

async function startCamera() {
  try {
    video = document.getElementById("video")
    canvas = document.getElementById("canvas")
    ctx = canvas.getContext("2d")
    const stream = await navigator.mediaDevices.getUserMedia({
      video: {
        facingMode: "environment",
        width: { ideal: 1920, min: 1280 },
        height: { ideal: 1080, min: 720 },
        focusMode: "continuous",
        exposureMode: "continuous",
      },
    })
    video.srcObject = stream
    console.log("✅ Auto-detection camera started")
  } catch (error) {
    console.error("Camera error:", error)
    updateStatus("❌ Camera access failed: " + error.message)
  }
}

async function startAutoDetection() {
  // Continuous auto-detection every 1.5 seconds
  setInterval(async () => {
    if (isModelLoaded && isOCRReady && video.readyState === 4 && !isProcessing && currentMode === "auto") {
      await performAutoDetectionAndCapture()
    }
  }, 1500)
}

async function performAutoDetectionAndCapture() {
  try {
    if (!ctx || !video || isProcessing) return

    canvas.width = video.videoWidth || 640
    canvas.height = video.videoHeight || 480

    // Run detection on full resolution video
    const predictions = await model.detect(video)

    // Look for all potential number plate carriers
    const plateCarriers = predictions.filter((p) => {
      const carriers = ["cell phone", "laptop", "tv", "book", "remote", "car", "truck", "bus", "motorcycle"]
      return carriers.includes(p.class) && p.score > 0.25
    })

    if (plateCarriers.length > 0) {
      // Try each detection in order of confidence
      const sortedCarriers = plateCarriers.sort((a, b) => b.score - a.score)
      for (const carrier of sortedCarriers) {
        const plateText = await extractABC1234FromCarrier(video, carrier)
        if (plateText && plateText !== lastDetectedPlate) {
          // Avoid duplicate processing
          if (Date.now() - lastDetectionTime < 3000) continue

          lastDetectedPlate = plateText
          lastDetectionTime = Date.now()
          liveDetectionCount++

          console.log(`🎯 AUTO-DETECTED ABC1234: ${plateText} from ${carrier.class}`)

          // Show detection and auto-capture
          updateLiveDetection(plateText, carrier.class, Math.round(carrier.score * 100))

          // Auto-capture and process immediately
          await autoCapture(plateText)
          break
        }
      }
    }

    // Clear old detection display
    if (Date.now() - lastDetectionTime > 5000) {
      clearLiveDetection()
    }
  } catch (error) {
    console.error("Auto-detection error:", error)
  }
}

async function autoCapture(detectedPlate) {
  if (isProcessing) return

  isProcessing = true
  try {
    console.log(`📸 Auto-capturing for plate: ${detectedPlate}`)
    showResult(`🔍 Auto-detected: ${detectedPlate} - Capturing and processing...`, "info")

    // Create high-quality capture
    const captureCanvas = document.createElement("canvas")
    captureCanvas.width = video.videoWidth || 640
    captureCanvas.height = video.videoHeight || 480
    const captureCtx = captureCanvas.getContext("2d")
    captureCtx.drawImage(video, 0, 0)

    const imageData = captureCanvas.toDataURL("image/jpeg", 0.9)

    // Submit to database immediately
    await submitDetection(detectedPlate, imageData)
  } catch (error) {
    console.error("Auto-capture error:", error)
    showResult(`❌ Auto-capture failed: ${error.message}`, "error")
  } finally {
    // Allow next detection after 3 seconds
    setTimeout(() => {
      isProcessing = false
    }, 3000)
  }
}

async function extractABC1234FromCarrier(video, carrier) {
  try {
    const [x, y, width, height] = carrier.bbox
    const extractCanvas = document.createElement("canvas")
    const padding = 20
    extractCanvas.width = width + padding * 2
    extractCanvas.height = height + padding * 2
    const extractCtx = extractCanvas.getContext("2d")

    extractCtx.drawImage(
      video,
      Math.max(0, x - padding),
      Math.max(0, y - padding),
      width + padding * 2,
      height + padding * 2,
      0,
      0,
      extractCanvas.width,
      extractCanvas.height,
    )

    // Try multiple enhancement strategies for ABC1234
    const strategies = [
      { name: "abc1234_sharp", scale: 4.0, threshold: 130, invert: false },
      { name: "abc1234_contrast", scale: 3.5, threshold: 110, invert: false },
      { name: "abc1234_inverted", scale: 4.0, threshold: 130, invert: true },
    ]

    for (const strategy of strategies) {
      const plateText = await tryABC1234OCR(extractCanvas, strategy)
      if (plateText) {
        console.log(`✅ ABC1234 Success with ${strategy.name}: ${plateText}`)
        return plateText
      }
    }

    return null
  } catch (error) {
    console.error("ABC1234 extraction error:", error)
    return null
  }
}

async function tryABC1234OCR(canvas, strategy) {
  try {
    const enhancedCanvas = document.createElement("canvas")
    enhancedCanvas.width = Math.floor(canvas.width * strategy.scale)
    enhancedCanvas.height = Math.floor(canvas.height * strategy.scale)
    const enhancedCtx = enhancedCanvas.getContext("2d")

    enhancedCtx.imageSmoothingEnabled = false
    enhancedCtx.drawImage(canvas, 0, 0, enhancedCanvas.width, enhancedCanvas.height)

    const imageData = enhancedCtx.getImageData(0, 0, enhancedCanvas.width, enhancedCanvas.height)
    applyABC1234Enhancement(imageData, strategy.threshold, strategy.invert)
    enhancedCtx.putImageData(imageData, 0, 0)

    const result = await tesseractWorker.recognize(enhancedCanvas)

    if (result.data.text && result.data.confidence > 10) {
      const plateText = extractABC1234Format(result.data.text, result.data.confidence)
      if (plateText) {
        return plateText
      }
    }

    return null
  } catch (error) {
    console.error("ABC1234 OCR strategy error:", error)
    return null
  }
}

function applyABC1234Enhancement(imageData, threshold = 130, invert = false) {
  const data = imageData.data

  // Convert to grayscale
  for (let i = 0; i < data.length; i += 4) {
    const gray = data[i] * 0.299 + data[i + 1] * 0.587 + data[i + 2] * 0.114
    data[i] = gray
    data[i + 1] = gray
    data[i + 2] = gray
  }

  // Apply threshold
  for (let i = 0; i < data.length; i += 4) {
    const gray = data[i]
    const enhanced = invert ? (gray < threshold ? 255 : 0) : gray > threshold ? 255 : 0
    data[i] = enhanced
    data[i + 1] = enhanced
    data[i + 2] = enhanced
  }
}

function extractABC1234Format(text, confidence) {
  if (!text) return null

  console.log(`🔍 OCR Text for ABC1234: "${text}" (${confidence}% confidence)`)

  const cleanText = text.replace(/[^A-Z0-9]/g, "").toUpperCase()
  console.log(`🧹 Cleaned ABC1234: "${cleanText}"`)

  // Primary pattern: exactly 3 letters followed by 4 numbers
  const primaryPattern = /^[A-Z]{3}\d{4}$/
  const primaryMatch = cleanText.match(primaryPattern)

  if (primaryMatch) {
    console.log(`✅ Perfect ABC1234 match: ${primaryMatch[0]}`)
    return primaryMatch[0]
  }

  // Secondary patterns
  const secondaryPatterns = [
    /[A-Z]{3}\d{4}/, // ABC1234 anywhere in text
    /^[A-Z]{3}\d{3}$/, // ABC123 (missing one digit)
    /^[A-Z]{2}\d{4}$/, // AB1234 (missing one letter)
  ]

  for (const pattern of secondaryPatterns) {
    const match = cleanText.match(pattern)
    if (match) {
      const found = match[0]
      // Try to complete partial matches
      if (found.length === 6 && /^[A-Z]{3}\d{3}$/.test(found)) {
        const remainingText = cleanText.replace(found, "")
        const extraDigits = remainingText.match(/\d/g)
        if (extraDigits && extraDigits.length > 0) {
          const completed = found + extraDigits[0]
          console.log(`✅ Completed ABC1234: ${completed}`)
          return completed
        }
      }

      if (found.length === 6 && /^[A-Z]{2}\d{4}$/.test(found)) {
        const remainingText = cleanText.replace(found, "")
        const extraLetters = remainingText.match(/[A-Z]/g)
        if (extraLetters && extraLetters.length > 0) {
          const completed = found.substring(0, 2) + extraLetters[0] + found.substring(2)
          console.log(`✅ Completed ABC1234: ${completed}`)
          return completed
        }
      }

      console.log(`✅ Secondary ABC1234 match: ${found}`)
      return found
    }
  }

  return null
}

function updateLiveDetection(plateId, source, confidence) {
  const liveDetectionDiv = document.getElementById("liveDetection")
  const plateIdSpan = document.getElementById("detectedPlateId")
  const sourceSpan = document.getElementById("detectionSource")
  const confidenceSpan = document.getElementById("detectionConfidence")
  const liveCountSpan = document.getElementById("liveDetectionCount")

  if (plateIdSpan) plateIdSpan.textContent = plateId
  if (sourceSpan) sourceSpan.textContent = `${source} (ABC1234)`
  if (confidenceSpan) confidenceSpan.textContent = confidence
  if (liveCountSpan) liveCountSpan.textContent = liveDetectionCount

  if (liveDetectionDiv) liveDetectionDiv.style.display = "block"

  // Update GPS display
  updateGPSDisplay()

  // Add success animation
  if (liveDetectionDiv) {
    liveDetectionDiv.classList.add("success-pulse")
    setTimeout(() => {
      liveDetectionDiv.classList.remove("success-pulse")
    }, 2000)
  }
}

function clearLiveDetection() {
  const liveDetectionDiv = document.getElementById("liveDetection")
  const plateIdSpan = document.getElementById("detectedPlateId")
  const sourceSpan = document.getElementById("detectionSource")
  const confidenceSpan = document.getElementById("detectionConfidence")

  if (plateIdSpan) plateIdSpan.textContent = "-"
  if (sourceSpan) sourceSpan.textContent = "-"
  if (confidenceSpan) confidenceSpan.textContent = "-"

  if (liveDetectionDiv) liveDetectionDiv.style.display = "none"

  lastDetectedPlate = null
}

async function submitDetection(plateId, imageData) {
  try {
    showResult(
      `
      <div class="alert alert-info">
        🔍 Auto-detected ABC1234: <strong>${plateId}</strong><br>
        📤 Checking database and saving...
        ${currentLocation.latitude ? `<br>📍 GPS: ${currentLocation.latitude.toFixed(6)}, ${currentLocation.longitude.toFixed(6)}` : ""}
      </div>
    `,
      "info",
    )

    const response = await fetch("/Scan/ProcessDetection", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        binPlateId: plateId,
        imageData: imageData,
        latitude: currentLocation.latitude || 0,
        longitude: currentLocation.longitude || 0,
        isManual: false,
      }),
    })

    const result = await response.json()

    if (result.success) {
      if (result.alreadyCollected) {
        showResult(
          `
          <div class="alert alert-warning">
            ⚠️ ${result.message}<br>
            <small>Plate: <strong>${result.binPlateId}</strong> - Last collected: ${result.collectionTime}</small>
          </div>
        `,
          "warning",
        )
      } else {
        showResult(
          `
          <div class="alert alert-success">
            <h5>✅ ${result.message}</h5>
            <hr>
            <div class="row">
              <div class="col-md-6">
                <p><strong>🤖 Detection Method:</strong> ${result.detectionMethod}</p>
                <p><strong>🔍 ABC1234 Plate:</strong> ${result.binPlateId}</p>
                <p><strong>📍 Location:</strong> ${result.binLocation || "Not specified"}</p>
                <p><strong>📅 Collection Time:</strong> ${result.collectionTime}</p>
                <p><strong>🌍 GPS:</strong> ${result.gpsLocation || "Not available"}</p>
              </div>
              <div class="col-md-6">
                <p><strong>🏢 Client:</strong> ${result.clientName || "Not specified"}</p>
                <p><strong>📋 Record ID:</strong> ${result.collectionRecordId}</p>
                <p><strong>🚛 Truck:</strong> ${result.truckLicensePlate || "Unknown"}</p>
                <p><strong>👤 Collector:</strong> ${result.collectorName || "Unknown"}</p>
              </div>
            </div>
          </div>
        `,
          "success",
        )
      }

      // Clear the detection after successful processing
      setTimeout(() => {
        clearLiveDetection()
        showResult("", "")
      }, 5000)
    } else {
      showResult(
        `
        <div class="alert alert-warning">
          ⚠️ ${result.message}
          <br><small>Detected plate: <strong>${plateId}</strong> - Continue scanning...</small>
        </div>
      `,
        "warning",
      )

      // Clear warning after 3 seconds and continue scanning
      setTimeout(() => {
        showResult("", "")
      }, 3000)
    }
  } catch (error) {
    console.error("Submit error:", error)
    showResult(
      `
      <div class="alert alert-danger">
        ❌ Connection failed: ${error.message}
        <br><small>Detected plate: <strong>${plateId}</strong> - Continue scanning...</small>
      </div>
    `,
      "error",
    )

    // Clear error after 3 seconds and continue scanning
    setTimeout(() => {
      showResult("", "")
    }, 3000)
  }
}

function showResult(message, type) {
  const resultDiv = document.getElementById("result")
  if (resultDiv) resultDiv.innerHTML = message
}

function updateStatus(message) {
  const statusDiv = document.getElementById("status")
  if (statusDiv) statusDiv.innerHTML = message
  console.log(message)
}

window.addEventListener("beforeunload", async () => {
  if (tesseractWorker) await tesseractWorker.terminate()
  if (video && video.srcObject) {
    video.srcObject.getTracks().forEach((track) => track.stop())
  }
  if (manualVideo && manualVideo.srcObject) {
    manualVideo.srcObject.getTracks().forEach((track) => track.stop())
  }
})

// Enhanced CSS
const style = document.createElement("style")
style.textContent = `
  .success-pulse {
    animation: successPulse 2s ease-in-out;
    border: 3px solid #28a745 !important;
  }
  
  @keyframes successPulse {
    0% { transform: scale(1); box-shadow: 0 0 0 0 rgba(40, 167, 69, 0.7); }
    50% { transform: scale(1.05); box-shadow: 0 0 0 15px rgba(40, 167, 69, 0); }
    100% { transform: scale(1); box-shadow: 0 0 0 0 rgba(40, 167, 69, 0); }
  }

  .alert-sm {
    padding: 0.5rem;
    margin-bottom: 0.5rem;
  }

  .btn-group .btn {
    transition: all 0.3s ease;
  }

  #manualPlateId {
    font-family: 'Courier New', monospace;
    font-weight: bold;
    font-size: 1.2rem;
  }

  .form-control:focus {
    border-color: #007bff;
    box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
  }
`
document.head.appendChild(style)
