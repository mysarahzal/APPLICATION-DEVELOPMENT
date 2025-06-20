from ultralytics import YOLO
import cv2
import pytesseract
import sys
import os

# Load YOLO model
model = YOLO("best.pt")  # Replace with your model path
img_path = sys.argv[1]

# Run detection
results = model(img_path)

# Get original image
image = cv2.imread(img_path)

# Find bounding box for class "BinPlate" (assuming class index 1 is 'BinPlate')
for result in results:
    boxes = result.boxes
    for box in boxes:
        cls_id = int(box.cls[0])
        if cls_id == 1:  # class 1 = BinPlate
            x1, y1, x2, y2 = map(int, box.xyxy[0])
            bin_plate_crop = image[y1:y2, x1:x2]
            # Optional: preprocess image
            gray = cv2.cvtColor(bin_plate_crop, cv2.COLOR_BGR2GRAY)
            _, thresh = cv2.threshold(gray, 150, 255, cv2.THRESH_BINARY)

            # Run OCR
            plate_text = pytesseract.image_to_string(thresh, config='--psm 7').strip()
            print(plate_text)  # ✅ This is what ASP.NET will receive
            sys.exit()

# If no plate found
print("❌ No plate detected")
